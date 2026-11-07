using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;

namespace NebulaSoft
{
    public static class Serializer
    {
        private static string persistentDataPath;
        private static readonly object fileWriteGate = new object();
        private static readonly HashSet<string> blockedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static bool IsWriteBlocked(string fileName)
        {
            lock (fileWriteGate) return blockedFiles.Contains(Path.GetFullPath(Path.Combine(GetPersistentDataPath(), fileName)));
        }

        public static void Init()
        {
            persistentDataPath = Application.persistentDataPath;
        }

        /// <summary>
        /// Deserializes file located at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of input file.</param>
        /// <returns>Deserialized object if file exists or new instance if doesn't.</returns>
        public static T Deserialize<T>(string fileName, bool logIfFileNotExists = false) where T : new()
        {
            string absolutePath = Path.GetFullPath(Path.Combine(GetPersistentDataPath(), fileName));
            lock (fileWriteGate)
            {
                if (!File.Exists(absolutePath) && !File.Exists(absolutePath + ".bak"))
                {
                    if (logIfFileNotExists) Debug.LogWarning("[Serializer] Save file does not exist: " + absolutePath);
                    return new T();
                }
                Exception primaryError;
                try
                {
                    T value = ReadFile<T>(absolutePath);
                    blockedFiles.Remove(absolutePath);
                    return value;
                }
                catch (NotSupportedException) { blockedFiles.Add(absolutePath); throw; }
                catch (Exception exception) { primaryError = exception; }
                try
                {
                    T value = ReadFile<T>(absolutePath + ".bak");
                    blockedFiles.Remove(absolutePath);
                    Debug.LogWarning("[Serializer] Recovered save from backup: " + fileName);
                    return value;
                }
                catch (Exception backupError)
                {
                    blockedFiles.Add(absolutePath);
                    throw new IOException("Save and backup could not be loaded. Writes are blocked to preserve the original files.",
                        new AggregateException(primaryError, backupError));
                }
            }
        }

        private static T ReadFile<T>(string path)
        {
            using (var file = File.OpenRead(path))
            {
                if (typeof(T) == typeof(GlobalSave) && file.ReadByte() == '{')
                {
                    file.Position = 0;
                    using (var reader = new StreamReader(file))
                        return (T)(object)GlobalSave.ImportFileData(JsonUtility.FromJson<GlobalSave.FileData>(reader.ReadToEnd()));
                }
                file.Position = 0;
                // Compatibility reader for files produced by existing releases.
                return (T)new BinaryFormatter { Binder = new LegacySaveBinder() }.Deserialize(file);
            }
        }

        private sealed class LegacySaveBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == typeof(GlobalSave).FullName) return typeof(GlobalSave);
                if (typeName == typeof(SavedDataContainer).FullName) return typeof(SavedDataContainer);
                if (typeName == typeof(SavedDataContainer[]).FullName) return typeof(SavedDataContainer[]);
                if (typeName.StartsWith("System.Collections.Generic.List`1[[NebulaSoft.SavedDataContainer,", StringComparison.Ordinal))
                    return typeof(List<SavedDataContainer>);
                return Type.GetType(typeName + ", " + assemblyName, true);
            }
        }

        /// <summary>
        /// Serializes file to Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of output file.</param>
        /// <param name="objectToSerialize">Reference to object that should be serialized.</param>
        // Synchronous durable writes for purchases. Errors must reach the caller.
        public static void SerializeChecked<T>(T value, string fileName)
        {
            lock (fileWriteGate)
            {
                string destination = Path.GetFullPath(Path.Combine(GetPersistentDataPath(), fileName));
                if (blockedFiles.Contains(destination))
                    throw new IOException("Writes are blocked because this save could not be recovered.");
                string temporary = destination + ".pending";
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        if (value is GlobalSave global)
                        {
                            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(global.ExportFileData()));
                            stream.Write(bytes, 0, bytes.Length);
                        }
                        else new BinaryFormatter().Serialize(stream, value);
                        stream.Flush(true);
                    }
                    if (File.Exists(destination))
                    {
                        bool validPrimary = false;
                        try { ReadFile<T>(destination); validPrimary = true; }
                        catch (NotSupportedException) { blockedFiles.Add(destination); throw; }
                        catch { /* Keep a recovered backup when replacing a damaged primary. */ }
                        File.Replace(temporary, destination, validPrimary ? destination + ".bak" : null);
                    }
                    else File.Move(temporary, destination);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
        }

        public static void Serialize<T>(T objectToSerialize, string fileName)
        {
            try
            {
                SerializeChecked(objectToSerialize, fileName);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Serializer]: Failed to serialize: " + exception.Message);
            }
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPDP(string fileName)
        {
            return File.Exists(Path.Combine(GetPersistentDataPath(), fileName));
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPath(string absolutePath)
        {
            return File.Exists(absolutePath);
        }

        /// <summary>
        /// Checks if file exists add specified directory.
        /// </summary>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists and false otherwise.</returns>
        public static bool FileExistsAtPath(string directoryPath, string fileName)
        {
            return File.Exists(Path.Combine(directoryPath, fileName));
        }

        /// <summary>
        /// Delete file at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        public static void DeleteFileAtPDP(string fileName)
        {
            string path = Path.GetFullPath(Path.Combine(GetPersistentDataPath(), fileName));
            lock (fileWriteGate)
            {
                File.Delete(path);
                File.Delete(path + ".bak");
                blockedFiles.Remove(path);
            }
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        public static void DeleteFileAtPath(string absolutePath)
        {
            File.Delete(absolutePath);
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        public static void DeleteFileAtPath(string directoryPath, string fileName)
        {
            File.Delete(Path.Combine(directoryPath, fileName));
        }

        private static string GetPersistentDataPath()
        {
            if (string.IsNullOrEmpty(persistentDataPath))
            {
                persistentDataPath = Application.persistentDataPath;

                return persistentDataPath;
            }

            return persistentDataPath;
        }
    }
}
