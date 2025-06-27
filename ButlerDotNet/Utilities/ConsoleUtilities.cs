using System.Text;

namespace ButlerDotNet.Utilities;

public static class ConsoleUtilities
{
    public static string ReadPassword()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo keyInfo;

        do
        {
            keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Out.Write("\b \b");
                }
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Out.Write("*");
                password.Append(keyInfo.KeyChar);
            }
        }
        while (keyInfo.Key != ConsoleKey.Enter);

        Console.Out.Write(Console.Out.NewLine);
        return password.ToString();
    }
}
