using System.Security.Cryptography;

namespace ArchCore.Utils {
	public static class Hashing {
		public static string MD5(string input) {
			
			var ue = new System.Text.UTF8Encoding();
			var bytes = ue.GetBytes(input);

			// encrypt bytes
			var md5 = new MD5CryptoServiceProvider();
			var hashBytes = md5.ComputeHash(bytes);

			// Convert the encrypted bytes back to a string (base 16)
			var hashString = "";

			for (var i = 0; i < hashBytes.Length; i++) {
				hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			}
			return hashString.PadLeft(32, '0');
		}
	}
}