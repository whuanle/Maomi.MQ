using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Maomi.MQ.Helpers
{
    public static class WorkIdGenerator
    {
        public static int GenerateWorkId()
        {
            try
            {
                // Attempt to generate work ID based on container information
                int containerWorkId = GenerateContainerWorkId();

                // Limit the container work ID to the desired range
                containerWorkId = containerWorkId % 1024;

                return containerWorkId;
            }
            catch (Exception ex)
            {
                // Handle exception if container information is not available (not running in a container)
                Console.WriteLine($"Failed to generate work ID based on container information: {ex.Message}");
            }

            // Fallback to generating a work ID using a different approach (e.g., hostname, random number)
            string hostname = Environment.MachineName;
            int randomWorkId = new Random().Next();

            string combinedInfo = hostname + randomWorkId.ToString();
            byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(combinedInfo));
            int fallbackWorkId = BitConverter.ToInt32(hashBytes, 0);

            // Limit the fallback work ID to the desired range
            fallbackWorkId = fallbackWorkId % 1024;

            Console.WriteLine($"Generated fallback work ID: {fallbackWorkId}");
            return fallbackWorkId; 
        }

        private static int GenerateContainerWorkId()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // For Linux containers, retrieve the container ID from '/proc/self/cgroup'
                string cgroupPath = "/proc/self/cgroup";
                string cgroupId = File.ReadLines(cgroupPath).First().Split(' ').First();

                byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cgroupId));
                int containerWorkId = BitConverter.ToInt32(hashBytes, 0);

                return containerWorkId;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // For Windows containers, retrieve the container ID from the environment variable 'CONTAINERID'
                string containerId = Environment.GetEnvironmentVariable("CONTAINERID");

                byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(containerId));
                int containerWorkId = BitConverter.ToInt32(hashBytes, 0);

                return containerWorkId;
            }
            else
            {
                throw new Exception("Unsupported operating system platform for container identification");
            }
        }
    }
}
