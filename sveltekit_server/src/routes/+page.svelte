<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import Check from "lucide-svelte/icons/check";
  import ChevronDown from "lucide-svelte/icons/chevron-down";
  import IconSettings from "lucide-svelte/icons/settings";
  import LoaderCircle from "lucide-svelte/icons/loader-circle";
  import CircleX from "lucide-svelte/icons/circle-x";
  import { tick } from "svelte";
  import * as Command from "$lib/components/ui/command";
  import * as Popover from "$lib/components/ui/popover";
  import * as Select from "$lib/components/ui/select";
  import { cn } from "$lib/utils.js";

  const ASPNET_UPLOAD_ENDPOINT = "http://localhost:3000/upload";

  type FileSource = { value: "device" | "cloud"; label: string };
  const fileSources: FileSource[] = [ {value: "device", label: "From Device"}, {value: "cloud", label: "From Cloud"} ];
  const formats = [ {value: "MP4", label: "MP4"}, {value: "MKV", label: "MKV"}, {value: "AVI", label: "AVI"}, {value: "MOV", label: "MOV"}, {value: "FLV", label: "FLV"} ];

  let selectedFileSource = $state<FileSource | null>(null);

  $effect(() => {
    selectedFileSource;

    if (selectedFileSource?.value === "device") {
      console.log("device selected");
      const fileInput = document.getElementById("filesInput") as HTMLInputElement;
      fileInput.click();
    } else if (selectedFileSource?.value === "cloud") {
      console.log("cloud selected");
    }
  })

  let selectedFormat = $state("MP4");
  let selectedCodec = $state("H264");

  let files: { file: File, outputFormat: string, processedVideo: number[] | null}[] = $state([]);

  function handleFilesChange(event: Event) {
    const filesWithOutputFormats = Array.from((event.target as HTMLInputElement).files || []).map((file) => ({ file, outputFormat: selectedFormat }));
    files = Array.from(filesWithOutputFormats || []);
  }

  async function handleRequest(file: File, outputFormat: string) {
    const formData = new FormData();
    formData.append("videoFile", file);
    formData.append("outputFormat", outputFormat);
    formData.append("codec", selectedCodec);

    return fetch(ASPNET_UPLOAD_ENDPOINT, {
      method: "POST",
      body: formData,
    }).then(response => {
      if (!response.ok) {
        throw new Error(`failed to process file: ${file.name}`);
      }
      console.log(`file processed successfully: ${file.name}`);
      console.log(response);
      return response;
    }).catch(error => console.error(error));
  }

  let isProcessing = $state(false);
async function handleSubmit() {
  isProcessing = true;

  const uploadPromises = files.map(async ({ file, outputFormat }, index) => {
    const response = await handleRequest(file, outputFormat);

    if (response.ok) {
      const processedVideo = await response.blob();
      files[index].processedVideo = processedVideo;
      // save processed video to disk
      const url = URL.createObjectURL(processedVideo);
      const a = document.createElement("a");
      a.href = url;
      a.download = file.name;
      a.click();

    } else {
      console.error("failed to process video", file.name);
    }
  });

  try {
    await Promise.all(uploadPromises);
    console.log("all files processed successfully");
  } catch (error) {
    console.error("one or more files failed to process:", error);
  }

  isProcessing = false;
}
</script>

<div class="flex justify-center mt-24">
  <Input class="hidden" id="filesInput" type="file" accept="video/mp4, video/mvk, video/avi" multiple  onchange={(e) => handleFilesChange(e)} />
  {#if files.length > 0}
    <div class="flex flex-col space-y-4">
    <div class="border rounded border-primary">
      <Button onclick={() => document.getElementById("file-input")!.click()}>
        Add More Files
      </Button>
      <div class="flex flex-col space-y-2">
        {#each files as file, index}
          <div class="flex flex-row space-x-2">
            <div class="file-info">
              <div>{file.file.name}</div>
              <div>{(file.file.size / (1024 * 1024)).toFixed(2)} MB</div>
            </div>
            <Select.Root type="single" bind:value={file.outputFormat}>
              <Select.Trigger>
                  {file.outputFormat}
              </Select.Trigger>
              <Select.Content>
                <Select.Group>
                  {#each formats as format}
                    <Select.Item value={format.value} label={format.label}
                      >{format.label}</Select.Item
                    >
                  {/each}
                </Select.Group>
              </Select.Content>
            </Select.Root>
            <Button variant="outline" size="icon" title="Settings" onclick={() => console.log("settings")}>
              <IconSettings />
            </Button>
            <Button variant="outline" size="icon" title="Remove File" onclick={() => files = files.filter((_, i) => i !== index)}>
              <CircleX/>
            </Button>
          </div>
        {/each}
      </div>
    </div>
    <div class="flex flex-row space-x-2">
      <span>Convert All ({files.length}) to:</span>
      <Select.Root
            type="single"
            bind:value={selectedFormat}
          >
        <Select.Trigger>
          <span>{selectedFormat}</span>
        </Select.Trigger>
        <Select.Content>
            {#each formats as format}
              <Select.Item value={format.value} label={format.label}
                >{format.label}</Select.Item
              >
            {/each}
        </Select.Content>
      </Select.Root>
      <Button onclick={handleSubmit}>
          {#if isProcessing}
          <svg
          class="animate-spin h-8 w-8 text-white"
          fill="none"
          viewBox="0 0 24 24"
        >
          <LoaderCircle />
        </svg>{:else}Compress Now{/if}</Button>
    </div>
    </div>
  {:else}
    <Select.Root type="single" onValueChange={(v) => selectedFileSource = fileSources.find((s) => s.value === v) || null}>
      <Select.Trigger>
        {selectedFileSource ? selectedFileSource.label : "Choose Files"}
      </Select.Trigger>
      <Select.Content>
        <Select.Group>
          {#each fileSources as fileSource}
            <Select.Item
              value={fileSource.value}
            >
              {fileSource.label}
            </Select.Item>
          {/each}
        </Select.Group>
      </Select.Content>
    </Select.Root>
  {/if}
</div>
