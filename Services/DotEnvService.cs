namespace api_node_reservas.Services;

public static class DotEnvService
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            int equalsIndex = trimmedLine.IndexOf('=');

            if (equalsIndex <= 0)
            {
                continue;
            }

            string key = trimmedLine[..equalsIndex].Trim();
            string value = trimmedLine[(equalsIndex + 1)..].Trim().Trim('"');

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
