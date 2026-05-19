using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ProjectLink
{
    [DisallowMultipleComponent]
    public class GeneratedUIMarker : MonoBehaviour
    {
        public string stableId;

        public static string ComputeId(string target, string path)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(target + "\x01" + path));
            return System.BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower();
        }
    }
}
