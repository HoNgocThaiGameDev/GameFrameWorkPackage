using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace NebulaSoft
{
    public static class Serializer
    {
        private static string persistentDataPath;
        private static readonly object fileWriteGate = new object();

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
            string absolutePath = Path.Combine(GetPersistentDataPath(), fileName);

            if (FileExistsAtPath(absolutePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(absolutePath, FileMode.Open);

                try
                {
                    T deserializedObject = (T)bf.Deserialize(file);

                    return deserializedObject;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                    return new T();
                }
                finally
                {
                    file.Close();
                }
            }
            else
            {
                if (logIfFileNotExists)
                {
                    Debug.LogWarning("File at path : \"" + absolutePath + "\" does not exist.");
                }
                return new T();
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
                string destination = Path.Combine(GetPersistentDataPath(), fileName);
                string temporary = destination + ".pending";
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        new BinaryFormatter().Serialize(stream, value);
                        stream.Flush(true);
                    }
                    if (File.Exists(destination)) File.Replace(temporary, destination, null);
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
            File.Delete(Path.Combine(GetPersistentDataPath(), fileName));
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
