namespace HSRGlobalMetadata.Utils;

using System;
using System.IO;

public static class PatternScanner {
    public static long FindPatternInFile(string filePath, ReadOnlySpan<byte> pattern, ReadOnlySpan<bool> mask) {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        byte[] fileData = File.ReadAllBytes(filePath);
        int patLen = pattern.Length;

        for (int i = 0; i <= fileData.Length - patLen; i++) {
            bool found = true;
            for (int j = 0; j < patLen; j++) {
                if (mask[j] && fileData[i + j] != pattern[j]) {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }

        return -1;
    }

    public static long FindPatternInFile(string filePath, string idaPattern) {
        string[] tokens = idaPattern.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        byte[] patternBytes = new byte[tokens.Length];
        bool[] maskBytes = new bool[tokens.Length];

        for (int i = 0; i < tokens.Length; i++) {
            if (tokens[i] == "??" || tokens[i] == "?") {
                patternBytes[i] = 0x00;
                maskBytes[i] = false;
            } else {
                patternBytes[i] = Convert.ToByte(tokens[i], 16);
                maskBytes[i] = true;
            }
        }

        return FindPatternInFile(filePath, patternBytes, maskBytes);
    }
}