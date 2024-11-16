<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import IconSettings from "lucide-svelte/icons/settings";
  import LoaderCircle from "lucide-svelte/icons/loader-circle";
  import CircleX from "lucide-svelte/icons/circle-x";
  import * as Select from "$lib/components/ui/select";
  import { goto } from "$app/navigation";
  import { inputFilesStore } from "../stores";

  const ASPNET_UPLOAD_ENDPOINT = "http://localhost:3000/upload";

  type FileSource = { value: "device" | "cloud"; label: string };
  const fileSources: FileSource[] = [
    { value: "device", label: "From Device" },
    { value: "cloud", label: "From Cloud" },
  ];
  const formats = [
    { value: "MP4", label: "MP4" },
    { value: "MKV", label: "MKV" },
    { value: "AVI", label: "AVI" },
    { value: "MOV", label: "MOV" },
    { value: "FLV", label: "FLV" },
  ];

  let selectedFileSource = $state<FileSource | null>(null);

  $effect(() => {
    selectedFileSource;

    if (selectedFileSource?.value === "device") {
      console.log("device selected");
      const fileInput = document.getElementById(
        "filesInput",
      ) as HTMLInputElement;
      fileInput.click();
    } else if (selectedFileSource?.value === "cloud") {
      console.log("cloud selected");
    }
  });

  let selectedFormat = $state("MP4");
  let selectedCodec = $state("H264");

  function handleFilesChange(event: Event) {
    const newFiles = Array.from(
      (event.target as HTMLInputElement).files || [],
    ).map((file) => ({
      inputFile: file,
      outputFormat: selectedFormat,
      compressedBinary: null,
    }));

    $inputFilesStore = [
      ...$inputFilesStore,
      ...newFiles.filter(
        (newFile) =>
          !$inputFilesStore.some(
            (existingFile) =>
              existingFile.inputFile.name === newFile.inputFile.name &&
              existingFile.inputFile.size === newFile.inputFile.size,
          ),
      ),
    ];
  }

  async function handleRequest(file: File, outputFormat: string) {
    const formData = new FormData();
    formData.append("videoFile", file);
    formData.append("outputFormat", outputFormat);
    formData.append("codec", selectedCodec);

    return fetch(ASPNET_UPLOAD_ENDPOINT, {
      method: "POST",
      body: formData,
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`failed to process file: ${file.name}`);
        }
        console.log(`file processed successfully: ${file.name}`);
        console.log(response);
        return response;
      })
      .catch((error) => console.error(error));
  }

  let isProcessing = $state(false);
  async function handleSubmit() {
    isProcessing = true;

    const uploadPromises = $inputFilesStore.map(
      async ({ inputFile, outputFormat }, index) => {
        const response = await handleRequest(inputFile, outputFormat);

        if (response?.ok) {
          $inputFilesStore[index].compressedBinary = await response.blob();
        } else {
          console.error("failed to process video", inputFile.name);
        }
      },
    );

    try {
      await Promise.all(uploadPromises);
      console.log("all files processed successfully");
      goto("/download");
    } catch (error) {
      console.error("one or more files failed to process:", error);
    }

    isProcessing = false;
  }
</script>

<div class="mt-24 w-1/3 mx-auto">
  <Input
    class="hidden"
    id="filesInput"
    type="file"
    accept="video/mp4, video/mvk, video/avi"
    multiple
    onchange={(e) => handleFilesChange(e)}
  />
  {#if $inputFilesStore.length > 0}
    <div class="flex flex-col space-y-4 mx-auto">
      <Button
        class="w-1/3"
        onclick={() => document.getElementById("filesInput")!.click()}
      >
        Add More Files
      </Button>
      <div class="p-4 border-2 rounded border-primary">
        <div class="flex flex-col space-y-2">
          {#each $inputFilesStore as file, index}
            <div class="flex flex-row space-x-16">
              <div class="file-info">
                <div>{file.inputFile.name}</div>
                <div>{(file.inputFile.size / (1024 * 1024)).toFixed(2)} MB</div>
              </div>
              <span>Output:</span>
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
              <Button
                class="m-2"
                size="icon"
                title="Settings"
                onclick={() => console.log("settings")}
              >
                <IconSettings />
              </Button>
              <Button
                class="m-2"
                size="icon"
                title="Remove File"
                onclick={() =>
                  ($inputFilesStore = $inputFilesStore.filter(
                    (_, i) => i !== index,
                  ))}
              >
                <CircleX />
              </Button>
            </div>
          {/each}
        </div>
      </div>
      <div class="flex flex-row space-x-2">
        {#if $inputFilesStore.length > 1}
          <span>Convert All ({$inputFilesStore.length}) to:</span>
          <Select.Root type="single" bind:value={selectedFormat}>
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
        {/if}

        <Button onclick={handleSubmit}>
          {#if isProcessing}
            <svg
              class="animate-spin h-8 w-8 text-white"
              fill="none"
              viewBox="0 0 24 24"
            >
              <LoaderCircle />
            </svg>{:else}Compress Now{/if}</Button
        >
      </div>
    </div>
  {:else}
    <Select.Root
      type="single"
      onValueChange={(v) =>
        (selectedFileSource = fileSources.find((s) => s.value === v) || null)}
    >
      <Select.Trigger>
        {selectedFileSource ? selectedFileSource.label : "Choose Files"}
      </Select.Trigger>
      <Select.Content>
        <Select.Group>
          {#each fileSources as fileSource}
            <Select.Item value={fileSource.value}>
              {fileSource.label}
            </Select.Item>
          {/each}
        </Select.Group>
      </Select.Content>
    </Select.Root>
  {/if}
</div>
