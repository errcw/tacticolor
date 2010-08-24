using System;
using System.IO;
using System.Xml.Serialization;

namespace Strategy.Library.Storage
{
    /// <summary>
    /// Stores data in XML format.
    /// </summary>
    /// <typeparam name="T">The type of object to store.</typeparam>
    public class XmlStoreable<T> : IStoreable
    {
        /// <summary>
        /// The name of the file in which to store this data.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The data stored by this object.
        /// </summary>
        public T Data { get; set; }

        public XmlStoreable(string fileName)
        {
            FileName = fileName;
        }

        public XmlStoreable(string fileName, T data)
        {
            FileName = fileName;
            Data = data;
        }

        public void Save(Stream stream)
        {
            _serializer.Serialize(stream, Data);
        }

        public void Load(Stream stream)
        {
            Data = (T)_serializer.Deserialize(stream);
        }

        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(T));
    }
}