using System;

namespace LibFileDownload
{
    public class DownloadToFile
    {
        public static async Task DownloadFileToPathAsync(string fileName, string outFullPath, CancellationTokenSource cts)
        {
            // Проверка не нулевых вхожных данных
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (string.IsNullOrEmpty(outFullPath)) throw new ArgumentNullException("fullPathWhereToSave");

            // Поток для сохранения файла
            await using var fileStream = File.OpenWrite(outFullPath);
            // Определение HttpClient
            using var httpClient = new HttpClient();

            try
            {
                // Получение ответа по факту прочтения заголовков (содержимое ещё не прочитано)
                using var responseFile = await httpClient.GetAsync(
                    fileName, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);

                // Проверка кода ответа
                responseFile.EnsureSuccessStatusCode();
                if (responseFile.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new System.Net.WebException($"Код ответа не соответствует ожидаемому ОК, полученный код ответа: {responseFile.StatusCode}");


                // ReadAsStreamAsync() -    Сериализует HTTP-содержимое и возвращает поток,
                //                          представляющий содержимое в асинхронной операции.
                await using var contentPart = await responseFile.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);

                var fileLen = responseFile.Content.Headers.ContentLength;
                if (fileLen == 0) throw new ArgumentException("File has no length!");


                // Определяем буфер (не знаю почему такой размер =)
                var buffer = new byte[1024 * 8 * 2];
                // Условно бесконечный цикл с выходов в случае чтения 0 байт
                while (true)
                {
                    int bytesRead = await contentPart.ReadAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    Console.WriteLine(bytesRead);

                    if (bytesRead == buffer.Length)
                    {
                        await fileStream.WriteAsync(buffer);
                    }
                    else
                    {
                        await fileStream.WriteAsync(buffer.Take(bytesRead).ToArray());
                    }
                }

            }
            catch (TaskCanceledException) when (cts.IsCancellationRequested)
            {
                throw new System.Net.WebException($"Загрузка была прервана пользователем!");
            }
            catch (TaskCanceledException)
            {
                throw new System.Net.WebException($"Не знаю чего случилось, но загрузка прервана!");
            }
        }
    }
}