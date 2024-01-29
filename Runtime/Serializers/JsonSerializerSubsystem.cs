using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Tetraizor.Systems.Save.Base;
using Tetraizor.DebugUtils;
using Tetraizor.Bootstrap.Base;

namespace Tetraizor.Systems.Save.Serializers
{
    public class JsonSerializer : SaveDataSerializerSubsystemBase
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

            if (!File.Exists(path))
                File.Create(path).Close();

            // Create Reader object.
            StreamReader streamReader = new StreamReader(path, true);

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

            streamReader.Close();

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

        public override string GetSystemName()
        {
            return "SaveSystem";
        }

        public override IEnumerator LoadSubsystem(IPersistentSystem system)
        {
            _serializerID = _serializerCount;
            _serializerCount++;

            return null;
        }

        // Write
        public override IEnumerator SerializeData<T>(ISaveData saveData, string path)
        {
            _isWriting = true;
            _progress = 0;

            string objectJSON = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            bool isWriteFinished = false;

            StreamWriter streamWriter = new StreamWriter(path, false);

            var writeTask = new Task(async () =>
            {
                try
                {
                    await streamWriter.WriteAsync(objectJSON);
                    isWriteFinished = true;
                }
                catch (Exception e)
                {
                    DebugBus.LogError(e.ToString());
                    streamWriter.Close();
                }
            });

            writeTask.Start();

            while (!isWriteFinished)
            {
                yield return null;
            }

            streamWriter.Close();

            isWriteFinished = true;

            _readResult = saveData;

            yield return null;

            _isWriting = false;
            _progress = 1;
        }

        #endregion
    }
}
