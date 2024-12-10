using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//options
var outputOption = new Option<FileInfo>(new[] { "--output", "-o" }, "File path and name");
var languageOption = new Option<string[]>(new[] { "--language", "-l" }, "List of programming languages") { IsRequired = true };
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Write the routing in a file as a comment");
var sortOption = new Option<string>(new[] { "--sort", "-s" }, "Order by ABC or by type");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Deleting empty rows");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "Writing the author's name in a note at the top of the file");


//bundle
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((FileInfo output, string[] languages, bool note, string sort, bool removeEmptyLines, string author) =>
{
    try
    {
        if (output == null || string.IsNullOrWhiteSpace(output.FullName) || !output.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Error: Output file must be a valid .txt file.");
            return;
        }

        if (languages == null || languages.Length == 0 || languages.Any(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine("Error: You must specify at least one programming language.");
            return;
        }

        if (!string.IsNullOrEmpty(sort) && !new[] { "abc", "type" }.Contains(sort.ToLower()))
        {
            Console.WriteLine("Error: Sort option must be either 'abc' or 'type' or left empty.");
            return;
        }

        // יצירת רשימת קבצים לעיבוד
        var allFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\debug\\") && !f.Contains("\\release\\"))
            .ToArray();
        var files = languages.Contains("all")
            ? allFiles
            : allFiles.Where(f => languages.Any(lang => f.EndsWith($".{lang}", StringComparison.OrdinalIgnoreCase))).ToArray();

        if (files.Length == 0)
        {
            Console.WriteLine("Error: No matching files found for the specified languages.");
            return;
        }

        // יצירת קובץ ה-bundle
        using (var writer = new StreamWriter(output.FullName))
        {
            var sortedFiles = sort.Equals("type", StringComparison.OrdinalIgnoreCase)
                ? files.OrderBy(f => Path.GetExtension(f)).ThenBy(f => f).ToArray() // by type
                : sort.Equals("abc", StringComparison.OrdinalIgnoreCase)
                ? files.OrderBy(f => f).ToArray() // by ABC
                : files;

            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"// Author: {author}");
            }

            foreach (var file in sortedFiles)
            {
                if (note)
                {
                    var relativePath = Path.GetFullPath(file);
                    writer.WriteLine($"// Source: {relativePath}");
                }

                // כתיבת תוכן הקובץ
                var content = File.ReadAllText(file);
                if (removeEmptyLines)
                {
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line));
                    content = string.Join("\n", nonEmptyLines);
                }
                writer.WriteLine(content);
            }
        }

        Console.WriteLine("Bundle file was created at: " + output.FullName);
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: Directory not found.");
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Error: Access denied to the specified path.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, outputOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);


//create-rsp
var createRspCommand = new Command("create-rsp", "Creating a response file with a prepared command");

createRspCommand.SetHandler(() =>
{
    try
    {
        Console.WriteLine("Where do you want the new file to be?");
        string outputPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(outputPath) || !Directory.Exists(Path.GetDirectoryName(outputPath)))
        {
            Console.WriteLine("Error: Invalid output path. Please provide a valid directory.");
            return;
        }

        Console.WriteLine("What languages do you want to bundle? If you want all, write 'all'.");
        string languageInput = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(languageInput))
        {
            Console.WriteLine("Error: You must specify at least one language or 'all'.");
            return;
        }
        string[] languages = languageInput.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (!languages.Contains("all") && languages.Any(string.IsNullOrWhiteSpace))
        {
            Console.WriteLine("Error: Invalid language input. Ensure all language entries are valid.");
            return;
        }

        Console.WriteLine("Do you want to write the path of each file in the new file? 'Y' or 'N'");
        bool note = GetBooleanInput();

        Console.WriteLine("Do you want to sort the files by 1.abc, 2.type, or 3.not sort at all?");
        int sortNum = GetIntegerInput(1, 3);
        string sort = sortNum switch
        {
            1 => "abc",
            2 => "type",
            3 => "",
            _ => ""
        };

        Console.WriteLine("Do you want to remove empty lines? 'Y' or 'N'");
        bool removeEmptyLines = GetBooleanInput();

        Console.WriteLine("Do you want to write the author's name at the top of the file? 'Y' or 'N'");
        bool includeAuthor = GetBooleanInput();
        string authorName = null;
        if (includeAuthor)
        {
            Console.WriteLine("Write the author's name:");
            authorName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(authorName))
            {
                Console.WriteLine("Error: Author name cannot be empty.");
                return;
            }
        }

        // יצירת ה-RESPONSE FILE
        var commandLines = new[]
        {
            $"--output \"{outputPath}\"",
            $"--language {string.Join(",", languages)}",
            note ? "--note" : "",
            !string.IsNullOrEmpty(sort) ? $"--sort \"{sort}\"" : "",
            removeEmptyLines ? "--remove-empty-lines" : ""
        };
        if (!string.IsNullOrEmpty(authorName))
        {
            commandLines = commandLines.Append($"--author \"{authorName}\"").ToArray();
        }

        Console.WriteLine("Where do you want the response file to be? Provide a full path including the file name (e.g., C:\\path\\mycli.rsp):");
        string responseFilePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(responseFilePath) || !responseFilePath.EndsWith(".rsp", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Error: Invalid file path. Please make sure to include a file name with .rsp extension.");
            return;
        }

        File.WriteAllLines(responseFilePath, commandLines.Where(line => !string.IsNullOrWhiteSpace(line)));
        Console.WriteLine($"Response file created at: {responseFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});


bool GetBooleanInput()
{
    while (true)
    {
        string input = Console.ReadLine().Trim().ToLower();
        if (input == "y" || input == "yes") return true;
        if (input == "n" || input == "no") return false;
        Console.WriteLine("Invalid input. Please enter 'Y' or 'N'.");
    }
}

int GetIntegerInput(int min, int max)
{
    while (true)
    {
        string input = Console.ReadLine();
        if (int.TryParse(input, out int result) && result >= min && result <= max)
        {
            return result;
        }
        Console.WriteLine($"Invalid input. Please enter a number between {min} and {max}.");
    }
}


//root
var rootCommand = new RootCommand("React command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);
