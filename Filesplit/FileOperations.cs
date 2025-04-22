using Filesplit.Model;
using System;
using System.Threading.Tasks;
using Units;

namespace Filesplit
{
    public static class FileOperations
    {
        public delegate void onProgressChanged(Progress progress);
        public static event onProgressChanged ProgressChanged;

        private const int BUFFER_SIZE = 1048576; // 1 MB
        public const string SPLITTED_FILENAME = "splitted-files.json";

        public static async Task SplitFileAsync(string sourceFile, string destinationPath, int chunkSizeMB)
        {
            long chunkSizeBytes = (long)ByteUnit.FromMB(chunkSizeMB).To(Unit.B).Length;

            Progress currentProgress = new Progress();
            string sourceFileHash;

            try
            {
                sourceFileHash = await Hash.CreateHashFromFileAsync(sourceFile);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create hash from source file: {ex.Message}");
            }

            Info currentInfo = new Info
            {
                Hash = sourceFileHash,
                TargetFileName = System.IO.Path.GetFileName(sourceFile),
            };

            using (System.IO.FileStream sourceStream = new System.IO.FileStream(sourceFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                long length = sourceStream.Length;
                currentInfo.Length = length;

                if (length < chunkSizeBytes)
                    throw new Exception("Chunk size is larger than file size. No split needed.");   

                int chunks = (int)Math.Ceiling((length / (double)chunkSizeBytes));
                string fileName = System.IO.Path.GetFileName(sourceFile);

                currentProgress.TotalBytes = length;

                long totalBytesRead = 0L;
                bool quit = false;
            
                for (int c = 0; c < chunks; c++)
                {
                    string chunkFileName = $"{fileName}.{c}";
                    string chunkPath = System.IO.Path.Combine(destinationPath, chunkFileName);
                    currentInfo.Files.Add(chunkFileName);
                    long chunkBytesRead = 0;
                    
                    using (System.IO.FileStream targetStream = new System.IO.FileStream(chunkPath, System.IO.FileMode.CreateNew, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
                    {
                        byte[] buffer = new byte[BUFFER_SIZE];

                        if (chunkBytesRead + buffer.Length > chunkSizeBytes)
                            buffer = new byte[chunkSizeBytes - chunkBytesRead];

                        long chunkSize = chunkSizeBytes;
                        if (c == chunks - 1)
                        {
                            long lastChunk = length % chunkSizeBytes;
                            if (lastChunk > 0)
                                chunkSize = lastChunk;
                        }

                        while (chunkBytesRead < chunkSizeBytes && totalBytesRead < length)
                        {
                            int read = await sourceStream.ReadAsync(buffer, 0, buffer.Length);

                            if (read == 0)
                            {
                                quit = true;
                                break;
                            }

                            chunkBytesRead += read;
                            totalBytesRead += read;

                             await targetStream.WriteAsync(buffer, 0, read);

                            currentProgress.CurrentProgress = Math.Round((chunkBytesRead / (double)chunkSize) * 100);
                            currentProgress.TotalProgress = Math.Round((totalBytesRead / (double)length) * 100);
                            currentProgress.TotalBytesProcessed = totalBytesRead;
                            currentProgress.CurrentBytesProcessed = chunkBytesRead;
                            currentProgress.CurrentFileBytes = chunkSize;
                            ProgressChanged?.Invoke(currentProgress);
                        }
                        
                        if (quit)
                        {
                            currentProgress.CurrentProgress = 100;
                            currentProgress.TotalProgress = 100;
                            currentProgress.TotalBytesProcessed = totalBytesRead;
                            currentProgress.CurrentBytesProcessed = chunkBytesRead;
                            currentProgress.CurrentFileBytes = chunkSize;
                            ProgressChanged?.Invoke(currentProgress);
                            break;
                        }
                    }
                }
            }

            System.IO.File.WriteAllText(System.IO.Path.Combine(destinationPath, SPLITTED_FILENAME), System.Text.Json.JsonSerializer.Serialize(currentInfo, new System.Text.Json.JsonSerializerOptions() { WriteIndented = true }));

            return;
        }

        public static async Task MergeFileAsync(string destinationFolder)
        {
            Progress currentProgress = new Progress();

            string splittedFilePath = System.IO.Path.Combine(destinationFolder, SPLITTED_FILENAME);
            if (!System.IO.File.Exists(splittedFilePath))
                throw new Exception("This directory is not valid! Please ensure it contains the file splitted-files.json, if you have not copied this file, you should do it!");

            Info splittedInfo = System.Text.Json.JsonSerializer.Deserialize<Info>(System.IO.File.ReadAllText(splittedFilePath));
            string mergeFileName = System.IO.Path.Combine(destinationFolder, splittedInfo.TargetFileName);
            long totalReadBytes = 0L;

            using (System.IO.FileStream targetFileStream = new System.IO.FileStream(mergeFileName, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
            {
                int count = 0;
                foreach (var file in splittedInfo.Files)
                {
                    using (System.IO.FileStream readFileStram = new System.IO.FileStream(System.IO.Path.Combine(destinationFolder, file), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    {
                        long readBytes = 0;
                        while (readBytes < readFileStram.Length)
                        {
                            byte[] buffer = new byte[BUFFER_SIZE];
                            int read = await readFileStram.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                                break;

                            readBytes += read;
                            totalReadBytes += read;

                            await targetFileStream.WriteAsync(buffer, 0, read);

                            currentProgress.CurrentProgress = Math.Round((readBytes / (double)readFileStram.Length) * 100);
                            currentProgress.TotalProgress = Math.Round((totalReadBytes / (double)splittedInfo.Length) * 100, 0);
                            currentProgress.TotalBytes = splittedInfo.Length;
                            currentProgress.CurrentBytesProcessed = readBytes;
                            currentProgress.CurrentFileBytes = readFileStram.Length;
                            currentProgress.TotalBytesProcessed = totalReadBytes;
                            ProgressChanged?.Invoke(currentProgress);
                        }
                    }

                    count++;
                    currentProgress.TotalProgress = Math.Round((totalReadBytes / (double)splittedInfo.Length) * 100, 0);
                    currentProgress.CurrentProgress = 100;
                    ProgressChanged?.Invoke(currentProgress);
                }                
            }

            // Verify hash from created file!
            try
            {
                string hash = await Hash.CreateHashFromFileAsync(mergeFileName);
                if (hash != splittedInfo.Hash)
                    throw new InvalidHashException("Merged file is corrupted, either there was an error while merging or the files were not properly copied to the target system!");
            }
            catch (InvalidHashException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create hash from merged file: {ex.Message}");
            }
        }
    }

    public class InvalidHashException : Exception
    {
        public InvalidHashException(string message) : base(message)
        {
        }
    }
}