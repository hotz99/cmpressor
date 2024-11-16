export interface InputFile {
  inputFile: File;
  outputFormat: string;
  compressedBinary: Blob | null;
}
