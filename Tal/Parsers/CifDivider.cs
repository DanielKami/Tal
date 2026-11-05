// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: CifDivider.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Parsers
{
 /// <summary>
 /// Splits a Crystallographic Information File (CIF) into individual data blocks.
 /// Each block starts with a "data_" directive and represents a separate structure
 /// or data set within the file.
 /// </summary>
    public static class CifDivider
    {
        #region Nested Types

 /// <summary>
 /// Represents a single CIF data block extracted from a CIF file.
 /// </summary>
        public class CifBlock
        {
 /// <summary>Gets or sets the name of the data block (without the "data_" prefix).</summary>
            public string Name { get; set; } = "";

 /// <summary>Gets or sets the raw content of the data block.</summary>
            public string RawData { get; set; } = "";
        }

        #endregion

        #region Public Methods

 /// <summary>
 /// Divides the content of a CIF file into individual data blocks.
 /// </summary>
 /// <param name="content">The complete content of the CIF file.</param>
 /// <param name="filePath">Optional file path used to generate a default block name when no "data_" blocks are found.</param>
 /// <returns>A list of CifBlock objects, each representing a separate data block.</returns>
 /// <remarks>
 /// The method searches for lines starting with "data_" to identify block boundaries.
 /// If no "data_" blocks are found, the entire content is treated as a single block.
 /// </remarks>
        public static List<CifBlock> Divide(string content, string filePath = "")
        {
            var blocks = new List<CifBlock>();

            if (string.IsNullOrEmpty(content))
                return blocks;

 // ⭐ NIE usuwaj emptych linii — zachowaj structure pliku
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            int startIndex = -1;
            string currentName = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("data_"))
                {
                    if (startIndex != -1)
                    {
 // Save afterbeforeni blok
                        var blockLines = new List<string>();
                        for (int j = startIndex; j < i; j++)
                            blockLines.Add(lines[j]);

                        blocks.Add(new CifBlock
                        {
                            Name = currentName,
                            RawData = string.Join("\n", blockLines)
                        });
                    }
                    startIndex = i;
                    currentName = trimmed.Substring(5).Trim();
                }
            }

 // Save ostatni blok
            if (startIndex != -1 && startIndex < lines.Length)
            {
                var blockLines = new List<string>();
                for (int j = startIndex; j < lines.Length; j++)
                    blockLines.Add(lines[j]);

                blocks.Add(new CifBlock
                {
                    Name = currentName,
                    RawData = string.Join("\n", blockLines)
                });
            }

 // Fallback — no data_
            if (blocks.Count == 0 && !string.IsNullOrEmpty(content))
            {
                blocks.Add(new CifBlock
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    RawData = content
                });
            }

            return blocks;
        }

        #endregion
    }
}