<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import Download from "lucide-svelte/icons/download";
  import { browser } from "$app/environment";
  import { goto } from "$app/navigation";
  import { inputFilesStore } from "../../stores";

  if (browser) {
    if (!$inputFilesStore) {
      console.log("inputFilesStore not found");
    }

    if ($inputFilesStore.length == 0) {
      console.log("inputFilesStore is empty");
      goto("/");
    }
  }
</script>

<div class="mt-24 w-1/3 mx-auto">
  <div class="flex flex-col space-y-4 mx-auto">
    <div class="p-4 border-2 rounded border-primary">
      <div class="flex flex-col space-y-2">
        {#each $inputFilesStore as file: InputFile}
          <div class="flex flex-row space-x-16">
            <div class="file-info">
              <div>{file.inputFile.name}</div>
              <div>{(file.inputFile.size / (1024 * 1024)).toFixed(2)} MB</div>
            </div>
            <a
              href={URL.createObjectURL(file.compressedBinary!)}
              download={file.inputFile.name}
            >
              <Button class="m-2">
                <Download />
              </Button>
            </a>
          </div>
        {/each}
      </div>
    </div>
  </div>
</div>
