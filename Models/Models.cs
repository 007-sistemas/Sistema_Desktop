namespace BiometricSystem.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public byte[]? FingerprintTemplate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class TimeRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Type { get; set; } = "Entrada"; // Entrada ou Saída
        public string? Notes { get; set; }
    }
}
