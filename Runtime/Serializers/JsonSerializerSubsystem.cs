using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Tetraizor.Bootstrap.Base;
using Tetraizor.Systems.Save.Base;
using Tetraizor.DebugUtils;

namespace Tetraizor.Systems.Save.Serializers
{
    public class JsonSerializer : SaveDataSerializerBase
    {
        #region Serializer Methods

        // Read
        public override IEnumerator DeserializeData<T>(string path)
        {
            // Reset progress.
            _isReading = true;
            _progress = 0;

            if (path == null || path == "")
                DebugBus.LogWarning("Path is null or empty. Might not function properly.");

            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);

            // Create Reader object.
            StreamReader streamReader = new StreamReader(fileStream, true);

            string contents = "";

            bool isReadFinished = false;

            // Read file.
            var readTask = new Task(async () =>
            {
                contents = await streamReader.ReadToEndAsync();

                isReadFinished = true;
            });

            readTask.Start();

            while (!isReadFinished)
            {
                yield return null;
            }

            fileStream.Flush();
            fileStream.Close();

            try
            {
                _readResult = JsonConvert.DeserializeObject<T>(contents);
            }
            catch (Exception e)
            {
                DebugBus.LogError(e.Message);
            }

            if (_readResult == null)
            {
                DebugBus.LogError("There was an error deserializing the result. File might be of other type or corrupted.");
            }

            yield return null;

            _isReading = false;
            _progress = 1;
        }

        // Write
        public override IEnumerator SerializeData<T>(ISaveData saveData, string path)
        {
            _isWriting = true;
            _progress = 0;

            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);

            string objectJSON = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            bool isWriteFinished = false;

            var writeTask = new Task(async () =>
            {
                try
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(objectJSON);
                    await fileStream.WriteAsync(info);

                    isWriteFinished = true;
                }
                catch (Exception e)
                {
                    DebugBus.LogError(e.ToString());

                    fileStream.Close();
                    fileStream.Dispose();
                }
            });

            writeTask.Start();

            while (!isWriteFinished)
            {
                yield return null;
            }

            fileStream.Dispose();
            fileStream.Close();

            _readResult = saveData;

            yield return null;

            _isWriting = false;
            _progress = 1;
        }

        #endregion
    }
}
