using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        string privateKeyXml = @"<RSAKeyValue><Modulus>2J5cjvVKSc7AvPzaP7PvEroP73TjctXsno3fQGdelOVp/lLm51BtDeN+MwPbM1UZJmAeLiyCXxQR/gtoK9RrI/3RoP7Sb1ElF6vklJLxao4N+P9IoeqSNKHKcgBoeA5GivbgwMK0Ev1kz7QRg+00lUlgKQp7u3oWoX/Ca0TzlTajZVUKSC7YaNiu1slBymViXIQkHPYzhzaKkn/gPZmwRk7PQopy0ZkXTjiBybFpEc71SHdz8N4kyi8EUfr+OmEseLwfC7uVDLmxX1UIlYPilq51ivvqx3j+buxwTrarfhPV37r2mGPfZx7kf3QPx5mCHbn3Oj0o3zwzF4ciZHDTQQ==</Modulus><Exponent>AQAB</Exponent><P>9D08AtKoot+6IRNiOr+nfie0x0GFHgMRMbxc3d1VAz5PnEkBqZZo9Kl3JrnBXiV94aF2LyVeh5cmAoO1TDyPDJRMlDXuWs9JmpHvdR3eCB6RdDsBcwUEEp9xk+MXelF5nkg383nK/cf/OC9go98lanY2J8W+OM5kdrl75wUu1D8=</P><Q>4wylDttqRx+8Ntbe8+JSYFljcdVaMXRUZioEyvWoZY8Zsm6VrgtDS9D1SGd4cVjDwyIXgj2RMSsmyssHr07UH63gwunsr1e2kR9aSs3MX9zRqZ6T4ynIQpL02NbHtx54CZuxd7xQIkUVeYVAN0mTttI0Bqit7x4f6uH8s5NIeH8=</Q><DP>tBg3GQnG6Zq5P8xwQUuzMNYMemT4yIGQezEe9UZQenzG7UH0JN3Q9J/FSVvtlwNkSCzr3aXbh3XPxEjkNZvuC2OK+DqwUGvLJVv4y05Du472yFL+JZcMQsMpV9g7JqnPWQR9pV4obWu86OwegZd4moEiO7+XHYIrGFR7ZYUv27c=</DP><DQ>OnAyu5Q10oCUjPjZAaSq1ymqss3bHQd4AaOnACYiZaFjV82msNktRQJPX4diNKpIGJ1Zt6fpCuK0ZVXEcJa1ekHiq8hRpv/Ieam8L6ywgavwOtwZ7EoAxUHVy0nctYEeHDr6fnr1lRx63oAxewlw/4ky4tPfkMAMLwRjakTqTjk=</DQ><InverseQ>GjK0kphd9V0IP0NAPa+mT1XxWn+8X3eZbTzL570geiCL92xhNswwbqjbYYrtrWj5HQHwhQfvKSKEKlsKyN8B0I6S1obQZCUd33UvMMfssQtSN41aN6RysolDK8PQQJV0S7m7jgSzavYqhyiEoHp5e2IvcwXQpn9T7wlz39Rnl+w=</InverseQ><D>CkQnGtdhN/JGjC5noZFDiV2sF6swhGbDGhibNhAwGVpuFZGUQ+453OilOIG8R8iGspznkUSmwR7QY3vi6gDdOw1ye/DXxFLSMTcCVFBKhMRKwTxIEvRtDXWFhLWkPdxfV4Kv0DLqGeRMfNOJ6Pi6/0BFAGzwxEfa/rhxXGgeKzofWkW6yIL+X5w7hmZEkivWSfXpY5Z+JInAlouwEKtrb71gxDUVWsYcwFjxoW5ZpvaRlFsHz3Y3RIBiHxM3SVfcZrmJnBI+Qvjx7ve17+bQur9tXokADeRQ+P9Y+BrmO/k+uKqLspLEDt5g3d/4pHb53+IyIAa9xsMTeI+vu+kNiQ==</D></RSAKeyValue>";

        string expiry = args.Length > 0 ? args[0] : "2027-08-01";
        string machineId = args.Length > 1 ? args[1] : "*";

        string payload = $"{{\"Expiry\":\"{expiry}\",\"MachineId\":\"{machineId}\"}}";

        using (var rsa = RSA.Create(2048))
        {
            rsa.FromXmlString(privateKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(payload);
            byte[] sigBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string signatureBase64 = Convert.ToBase64String(sigBytes);

            string licenseKey = payload + "." + signatureBase64;
            Console.WriteLine("==========================================");
            Console.WriteLine("ÜRETİLEN LİSANS ANAHTARI:");
            Console.WriteLine("==========================================");
            Console.WriteLine(licenseKey);
            Console.WriteLine("==========================================");

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BigLineconnect");
            Directory.CreateDirectory(appDataPath);
            string keyFile = Path.Combine(appDataPath, "license.key");
            File.WriteAllText(keyFile, licenseKey);
            Console.WriteLine($"\n[BAŞARILI] Lisans bilgisayarınıza yüklendi ve kaydedildi: {keyFile}");
        }
    }
}
