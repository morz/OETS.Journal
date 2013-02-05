using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace OETS.Shared.Util
{
	public class BinarySerialization
	{
        /// <summary>
        /// Serialize the object into the file. 
        /// Exceptions are not caught.
        /// </summary>
		public static void Serialize(string filename, object obj)
		{
            string dir = Path.GetDirectoryName(filename);
            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            if (dirInfo.Exists == false)
                dirInfo.Create();

			using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
			{
				Serialize(fs, obj);
				fs.Close();
			}
		}

        /// <summary>
        /// Serialize the object to the specified stream. 
        /// Exceptions are not caught.
        /// </summary>
		public static void Serialize(Stream stream, object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(stream, obj);
		}

        /// <summary>
        /// Deserialize from file.
        /// Exceptions are not caught.
        /// </summary>
		public static object Deserialize(string filename)
		{
			object tmp;

			using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				tmp = Deserialize(fs);
				fs.Close();
			}
			return tmp;
		}

        /// <summary>
        /// Deserialize from stream.
        /// Exceptions are not caught.
        /// </summary>
		public static object Deserialize(Stream stream)
		{
			object tmp;
			BinaryFormatter bf = new BinaryFormatter();
			tmp = bf.Deserialize(stream);
			return tmp;
		}
	}
}