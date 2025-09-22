namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// Login statistics for a user
    /// </summary>
    public class LoginStatistics
    {
        /// <summary>
        /// Total number of login attempts
        /// </summary>
        public int TotalAttempts { get; set; }

        /// <summary>
        /// Number of successful logins
        /// </summary>
        public int SuccessfulLogins { get; set; }

        /// <summary>
        /// Number of failed login attempts
        /// </summary>
        public int FailedAttempts { get; set; }

        /// <summary>
        /// Number of unique IP addresses
        /// </summary>
        public int UniqueIpAddresses { get; set; }

        /// <summary>
        /// Number of unique devices
        /// </summary>
        public int UniqueDevices { get; set; }

        /// <summary>
        /// Average session duration in minutes
        /// </summary>
        public double AverageSessionDuration { get; set; }

        /// <summary>
        /// Last successful login date
        /// </summary>
        public DateTime? LastSuccessfulLogin { get; set; }

        /// <summary>
        /// Last failed login date
        /// </summary>
        public DateTime? LastFailedLogin { get; set; }

        /// <summary>
        /// Most used IP address
        /// </summary>
        public string? MostUsedIpAddress { get; set; }

        /// <summary>
        /// Most used device
        /// </summary>
        public string? MostUsedDevice { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulLogins / TotalAttempts * 100 : 0;
    }
}