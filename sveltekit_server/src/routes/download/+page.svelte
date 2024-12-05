<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import Download from "lucide-svelte/icons/download";
  import { browser } from "$app/environment";
  import { goto } from "$app/navigation";
  import { inputFilesStore } from "../../stores";

  if (browser && $inputFilesStore.length === 0) {
    goto("/");
  }

  function computeOutputFileName(inputFile: File, outputFormat: string) {
    return (
      inputFile.name.replace(/\.[^/.]+$/, "") + "." + outputFormat.toLowerCase()
    );
  }

  let outputFileNames: string[] = [];
  for (const file of $inputFilesStore) {
    outputFileNames.push(
      computeOutputFileName(file.inputFile, file.outputFormat),
    );
  }
</script>

<div class="mt-24 w-1/3 mx-auto">
  <div class="flex flex-col space-y-4 mx-auto">
    <div class="flex flex-col space-y-2 p-4 border-2 rounded border-primary">
      {#each $inputFilesStore as file: InputFile, index}
        <div class="flex flex-row justify-between items-center">
          <div class="w-2/3 file-info">
            <div>{outputFileNames[index]}</div>
            <div>
              {(file.compressedBinary!.size / (1024 * 1024)).toFixed(2)} MB
            </div>
          </div>
          <Button
            onclick={() => {
              const a = document.createElement("a");
              a.href = URL.createObjectURL(file.compressedBinary!);
              a.download = outputFileNames[index];
              a.click();
            }}
          >
            <Download />
          </Button>
        </div>
      {/each}
    </div>
  </div>
</div>
