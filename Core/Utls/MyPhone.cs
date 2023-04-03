namespace Utls;

public static class MyPhone
{
    public static int MinLength { get; set; } = 10;
    public static int MaxLength { get; set; } = 11;

    public static bool VerifyPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
        {
            return false;
        }

        // Remove any non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle the "+60" prefix for Malaysian phone numbers
        if (digitsOnly.StartsWith("60") && (digitsOnly.Length == MinLength + 1 || digitsOnly.Length == MaxLength + 1))
        {
            digitsOnly = "0" + digitsOnly[2..];
        }

        // Check if the phone number has the correct length and starts with the prefix "01"
        return (digitsOnly.Length >= MinLength && digitsOnly.Length <= MaxLength) && digitsOnly.StartsWith("01");
    }

    public static string NormalizePhoneNumber(string phoneNumber)
    {
        if (!VerifyPhoneNumber(phoneNumber))
        {
            throw new ArgumentException("Invalid Malaysian phone number format.");
        }

        // Remove any non-digit characters
        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Handle the "+60" prefix for Malaysian phone numbers
        if (digitsOnly.StartsWith("60") && (digitsOnly.Length == MinLength + 1 || digitsOnly.Length == MaxLength + 1))
        {
            digitsOnly = "0" + digitsOnly[2..];
        }

        return digitsOnly;
    }
}
