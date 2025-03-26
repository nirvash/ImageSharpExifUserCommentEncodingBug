﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats; // Image.Load<Rgba32> のために追加
using System.Linq; // For SequenceEqual
using MetadataExtractor; // Added for verification
using MetadataExtractor.Formats.Exif; // Added for verification

// --- Configuration ---
string inputPath = "image.webp"; // Assumed to exist
string outputPath = "output.webp";
// Use a string containing both ASCII and non-ASCII characters for the bug report
string comment = "Hello, World! こんにちわ世界"; // Define the comment string once at the beginning

Console.WriteLine($"Input image: {inputPath}");
Console.WriteLine($"Output image: {outputPath}");
Console.WriteLine($"Comment to set/verify: {comment}");

// --- Load and Process Image using ImageSharp (Simplified) ---
// Assuming inputPath exists and is a valid image
using var image = Image.Load<Rgba32>(inputPath);
Console.WriteLine($"Loaded image: {inputPath}");

// Create EXIF profile if it doesn't exist
var exif = image.Metadata.ExifProfile ?? new ExifProfile();

// Set Unicode text in UserComment using the predefined variable
exif.SetValue(ExifTag.UserComment, comment);
Console.WriteLine($"Setting EXIF UserComment to: {comment}");

// Apply EXIF profile to the image
image.Metadata.ExifProfile = exif;

// Save the image (Simplified)
image.Save(outputPath);
Console.WriteLine($"Saved image with EXIF data to: {outputPath}");


// --- Verification Step using MetadataExtractor (Simplified) ---
Console.WriteLine("\nVerifying EXIF UserComment raw bytes using MetadataExtractor...");

byte[] expectedPrefix = System.Text.Encoding.ASCII.GetBytes("UNICODE\0");
byte[] expectedLEBytes = System.Text.Encoding.Unicode.GetBytes(comment); // UTF-16 LE
byte[] expectedBEBytes = System.Text.Encoding.BigEndianUnicode.GetBytes(comment); // UTF-16 BE (Correct Spec)

Console.WriteLine($"Expected Prefix (ASCII \"UNICODE\\0\"): {BitConverter.ToString(expectedPrefix)}");
Console.WriteLine($"Expected Payload (UTF-16 LE - Incorrect): {BitConverter.ToString(expectedLEBytes)}");
Console.WriteLine($"Expected Payload (UTF-16 BE - Correct Spec): {BitConverter.ToString(expectedBEBytes)}");

try
{
    // Read metadata using MetadataExtractor
    var directories = ImageMetadataReader.ReadMetadata(outputPath);

    // Find the Exif SubIFD directory (assuming it exists)
    var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().First(); // Use First() assuming it exists

    // Get the UserComment byte array (assuming it exists and is byte[])
    byte[] actualBytes = exifSubIfdDirectory.GetByteArray(ExifDirectoryBase.TagUserComment)!; // Use ! assuming it's not null

    Console.WriteLine($"Actual Bytes Read by MetadataExtractor ({actualBytes.Length} bytes): {BitConverter.ToString(actualBytes)}");

    // Verify Prefix (assuming bytes are long enough)
    if (actualBytes.Take(8).SequenceEqual(expectedPrefix))
    {
        Console.WriteLine("Verification: Prefix 'UNICODE\\0' matches.");

        // Verify Payload
        byte[] actualPayloadBytes = actualBytes.Skip(8).ToArray();
        Console.WriteLine($"Actual Payload Bytes ({actualPayloadBytes.Length} bytes): {BitConverter.ToString(actualPayloadBytes)}");

        if (actualPayloadBytes.SequenceEqual(expectedLEBytes))
        {
            Console.WriteLine("Verification: Payload matches UTF-16 LE bytes. (BUG CONFIRMED - Should be UTF-16 BE)");
        }
        else if (actualPayloadBytes.SequenceEqual(expectedBEBytes))
        {
            Console.WriteLine("Verification: Payload matches UTF-16 BE bytes. (Correct according to spec)");
        }
        else
        {
            Console.WriteLine("Verification FAILED: Payload does not match expected UTF-16 LE or BE bytes.");
        }
    }
    else
    {
        Console.WriteLine("Verification FAILED: Prefix 'UNICODE\\0' not found or incorrect.");
    }
}
catch (Exception ex)
{
    // Catch potential errors during metadata reading or if assumptions fail
    Console.WriteLine($"Error during verification with MetadataExtractor: {ex.Message}");
}
// --- End of Verification Step ---

Console.WriteLine("\nProcessing complete.");


/*
Expected Output (assuming image.webp exists, contains EXIF, and ImageSharp writes UTF-16 LE for UserComment):

Input image: image.webp
Output image: output.webp
Comment to set/verify: Hello, World! こんにちわ世界
Loaded image: image.webp
Setting EXIF UserComment to: Hello, World! こんにちわ世界
Saved image with EXIF data to: output.webp

Verifying EXIF UserComment raw bytes using MetadataExtractor...
Expected Prefix (ASCII "UNICODE\0"): 55-4E-49-43-4F-44-45-00
Expected Payload (UTF-16 LE - Incorrect): 48-00-65-00-6C-00-6C-00-6F-00-2C-00-20-00-57-00-6F-00-72-00-6C-00-64-00-21-00-20-00-53-30-93-30-6B-30-61-30-8F-30-16-4E-4C-75-4C-75
Expected Payload (UTF-16 BE - Correct Spec): 00-48-00-65-00-6C-00-6C-00-6F-00-2C-00-20-00-57-00-6F-00-72-00-6C-00-64-00-21-00-20-30-53-30-93-30-6B-30-61-30-8F-4E-16-75-4C-75-4C
Actual Bytes Read by MetadataExtractor (... bytes): 55-4E-49-43-4F-44-45-00-48-00-65-00-6C-00-6C-00-6F-00-2C-00-20-00-57-00-6F-00-72-00-6C-00-64-00-21-00-20-00-53-30-93-30-6B-30-61-30-8F-30-16-4E-4C-75-4C-75
Verification: Prefix 'UNICODE\0' matches.
Actual Payload Bytes (... bytes): 48-00-65-00-6C-00-6C-00-6F-00-2C-00-20-00-57-00-6F-00-72-00-6C-00-64-00-21-00-20-00-53-30-93-30-6B-30-61-30-8F-30-16-4E-4C-75-4C-75
Verification: Payload matches UTF-16 LE bytes. (BUG CONFIRMED - Should be UTF-16 BE)

Processing complete.

*/
