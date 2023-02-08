using System;
using System.Security.Cryptography.X509Certificates;

namespace LibFileDownload
{
    public class DownloadToFile
    {
        public static async Task DownloadFileToPathAsync(string fileName, string outFullPath, CancellationToken token, ProgressStatus progressStatus)
        {
            // Проверка не нулевых вхожных данных
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException($"Неверно указан путь к файлу fileName: {fileName}");
            if (string.IsNullOrEmpty(outFullPath)) throw new ArgumentNullException($"Неверно указано имя файла outFullPath: {outFullPath}");

            // Поток для сохранения файла
            await using var fileStream = File.OpenWrite(outFullPath);
            // Определение HttpClient
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, fileName)
            {
                Headers = { Range = new System.Net.Http.Headers.RangeHeaderValue((long?)progressStatus.ActBytes, null) }
            };

            try
            {
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

                // Получение ответа по факту прочтения заголовков (содержимое ещё не прочитано)
                //using var responseFile = await httpClient.GetAsync(
                //    fileName, HttpCompletionOption.ResponseHeadersRead, token);

                // Проверка кода ответа
                if (response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    throw new System.Net.WebException($"Код ответа не соответствует ожидаемому PartialContent, полученный код ответа: {response.StatusCode}");


                // ReadAsStreamAsync() -    Сериализует HTTP-содержимое и возвращает поток,
                //                          представляющий содержимое в асинхронной операции.
                await using var contentPart = await response.Content.ReadAsStreamAsync(token);

                var fileLen = response.Content.Headers.ContentLength;
                if (fileLen == 0) throw new ArgumentException("File has no length!");
                progressStatus.TotalBytes = (long) fileLen;


                // Определяем буфер (не знаю почему такой размер =)
                var buffer = new byte[1024 * 8 * 2];
                // Условно бесконечный цикл с выходов в случае чтения 0 байт
                while (true)
                {
                    int bytesRead = await contentPart.ReadAsync(buffer, token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    if (bytesRead == buffer.Length)
                    {
                        await fileStream.WriteAsync(buffer, token);
                    }
                    else
                    {
                        await fileStream.WriteAsync(buffer.Take(bytesRead).ToArray(), token);
                    }
                    progressStatus.Add(bytesRead);
                }

            }
            catch (TaskCanceledException) when (token.IsCancellationRequested)
            {
                throw new System.Net.WebException($"Загрузка была прервана пользователем!");
            }
            catch (TaskCanceledException)
            {
                throw new System.Net.WebException($"Не знаю чего случилось, но загрузка прервана!");
            }
        }

        public class ProgressStatus
        {
            public delegate void ProgressStatusHandler(ProgressStatus sender, ProgressStatusEventArgs e);
            public event ProgressStatusHandler? Notify; // Определение события


            public long TotalBytes { get; set; }
            public float ActBytes { get; set; }
            public float Add(float actBytes)
            {
                ActBytes += actBytes;
                Notify?.Invoke(this, new ProgressStatusEventArgs((float)ActBytes / TotalBytes));
                return ((float)ActBytes / TotalBytes);
            }
        }
        public class ProgressStatusEventArgs
        {
            public float CurStatus { get; }
            public ProgressStatusEventArgs(float curStatus)
            {
                CurStatus = curStatus;
            }
        }
    }
}