using System;
using System.IO;
using System.Security.AccessControl;
using System.Text;
namespace Jolt.GeneratedTypes.System.IO {

	public class FileProxy : IFile {
		// Methods
		public void AppendAllText(string path, string contents) {
			File.AppendAllText(path, contents);
		}

		public void AppendAllText(string path, string contents, Encoding encoding) {
			File.AppendAllText(path, contents, encoding);
		}

		public StreamWriter AppendText(string path) {
			return File.AppendText(path);
		}

		public void Copy(string sourceFileName, string destFileName) {
			File.Copy(sourceFileName, destFileName);
		}

		public void Copy(string sourceFileName, string destFileName, bool overwrite) {
			File.Copy(sourceFileName, destFileName, overwrite);
		}

		public FileStream Create(string path) {
			return File.Create(path);
		}

		public FileStream Create(string path, int bufferSize) {
			return File.Create(path, bufferSize);
		}

		public FileStream Create(string path, int bufferSize, FileOptions options) {
			return File.Create(path, bufferSize, options);
		}

		public FileStream Create(string path, int bufferSize, FileOptions options, FileSecurity fileSecurity) {
			return File.Create(path, bufferSize, options, fileSecurity);
		}

		public StreamWriter CreateText(string path) {
			return File.CreateText(path);
		}

		public void Decrypt(string path) {
			File.Decrypt(path);
		}

		public void Delete(string path) {
			File.Delete(path);
		}

		public void Encrypt(string path) {
			File.Encrypt(path);
		}

		public bool Exists(string path) {
			return File.Exists(path);
		}

		public FileSecurity GetAccessControl(string path) {
			return File.GetAccessControl(path);
		}

		public FileSecurity GetAccessControl(string path, AccessControlSections includeSections) {
			return File.GetAccessControl(path, includeSections);
		}

		public FileAttributes GetAttributes(string path) {
			return File.GetAttributes(path);
		}

		public DateTime GetCreationTime(string path) {
			return File.GetCreationTime(path);
		}

		public DateTime GetCreationTimeUtc(string path) {
			return File.GetCreationTimeUtc(path);
		}

		public DateTime GetLastAccessTime(string path) {
			return File.GetLastAccessTime(path);
		}

		public DateTime GetLastAccessTimeUtc(string path) {
			return File.GetLastAccessTimeUtc(path);
		}

		public DateTime GetLastWriteTime(string path) {
			return File.GetLastWriteTime(path);
		}

		public DateTime GetLastWriteTimeUtc(string path) {
			return File.GetLastWriteTimeUtc(path);
		}

		public void Move(string sourceFileName, string destFileName) {
			File.Move(sourceFileName, destFileName);
		}

		public FileStream Open(string path, FileMode mode) {
			return File.Open(path, mode);
		}

		public FileStream Open(string path, FileMode mode, FileAccess access) {
			return File.Open(path, mode, access);
		}

		public FileStream Open(string path, FileMode mode, FileAccess access, FileShare share) {
			return File.Open(path, mode, access, share);
		}

		public FileStream OpenRead(string path) {
			return File.OpenRead(path);
		}

		public StreamReader OpenText(string path) {
			return File.OpenText(path);
		}

		public FileStream OpenWrite(string path) {
			return File.OpenWrite(path);
		}

		public byte[] ReadAllBytes(string path) {
			return File.ReadAllBytes(path);
		}

		public string[] ReadAllLines(string path) {
			return File.ReadAllLines(path);
		}

		public string[] ReadAllLines(string path, Encoding encoding) {
			return File.ReadAllLines(path, encoding);
		}

		public string ReadAllText(string path) {
			return File.ReadAllText(path);
		}

		public string ReadAllText(string path, Encoding encoding) {
			return File.ReadAllText(path, encoding);
		}

		public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName) {
			File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);
		}

		public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) {
			File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
		}

		public void SetAccessControl(string path, FileSecurity fileSecurity) {
			File.SetAccessControl(path, fileSecurity);
		}

		public void SetAttributes(string path, FileAttributes fileAttributes) {
			File.SetAttributes(path, fileAttributes);
		}

		public void SetCreationTime(string path, DateTime creationTime) {
			File.SetCreationTime(path, creationTime);
		}

		public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) {
			File.SetCreationTimeUtc(path, creationTimeUtc);
		}

		public void SetLastAccessTime(string path, DateTime lastAccessTime) {
			File.SetLastAccessTime(path, lastAccessTime);
		}

		public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) {
			File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);
		}

		public void SetLastWriteTime(string path, DateTime lastWriteTime) {
			File.SetLastWriteTime(path, lastWriteTime);
		}

		public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) {
			File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);
		}

		public void WriteAllBytes(string path, byte[] bytes) {
			File.WriteAllBytes(path, bytes);
		}

		public void WriteAllLines(string path, string[] contents) {
			File.WriteAllLines(path, contents);
		}

		public void WriteAllLines(string path, string[] contents, Encoding encoding) {
			File.WriteAllLines(path, contents, encoding);
		}

		public void WriteAllText(string path, string contents) {
			File.WriteAllText(path, contents);
		}

		public void WriteAllText(string path, string contents, Encoding encoding) {
			File.WriteAllText(path, contents, encoding);
		}
	}




	public interface IFile {
		// Methods
		void AppendAllText(string path, string contents);
		void AppendAllText(string path, string contents, Encoding encoding);
		StreamWriter AppendText(string path);
		void Copy(string sourceFileName, string destFileName);
		void Copy(string sourceFileName, string destFileName, bool overwrite);
		FileStream Create(string path);
		FileStream Create(string path, int bufferSize);
		FileStream Create(string path, int bufferSize, FileOptions options);
		FileStream Create(string path, int bufferSize, FileOptions options, FileSecurity fileSecurity);
		StreamWriter CreateText(string path);
		void Decrypt(string path);
		void Delete(string path);
		void Encrypt(string path);
		bool Exists(string path);
		FileSecurity GetAccessControl(string path);
		FileSecurity GetAccessControl(string path, AccessControlSections includeSections);
		FileAttributes GetAttributes(string path);
		DateTime GetCreationTime(string path);
		DateTime GetCreationTimeUtc(string path);
		DateTime GetLastAccessTime(string path);
		DateTime GetLastAccessTimeUtc(string path);
		DateTime GetLastWriteTime(string path);
		DateTime GetLastWriteTimeUtc(string path);
		void Move(string sourceFileName, string destFileName);
		FileStream Open(string path, FileMode mode);
		FileStream Open(string path, FileMode mode, FileAccess access);
		FileStream Open(string path, FileMode mode, FileAccess access, FileShare share);
		FileStream OpenRead(string path);
		StreamReader OpenText(string path);
		FileStream OpenWrite(string path);
		byte[] ReadAllBytes(string path);
		string[] ReadAllLines(string path);
		string[] ReadAllLines(string path, Encoding encoding);
		string ReadAllText(string path);
		string ReadAllText(string path, Encoding encoding);
		void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName);
		void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
		void SetAccessControl(string path, FileSecurity fileSecurity);
		void SetAttributes(string path, FileAttributes fileAttributes);
		void SetCreationTime(string path, DateTime creationTime);
		void SetCreationTimeUtc(string path, DateTime creationTimeUtc);
		void SetLastAccessTime(string path, DateTime lastAccessTime);
		void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);
		void SetLastWriteTime(string path, DateTime lastWriteTime);
		void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);
		void WriteAllBytes(string path, byte[] bytes);
		void WriteAllLines(string path, string[] contents);
		void WriteAllLines(string path, string[] contents, Encoding encoding);
		void WriteAllText(string path, string contents);
		void WriteAllText(string path, string contents, Encoding encoding);
	}
}

