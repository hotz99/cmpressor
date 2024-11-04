<script lang="ts">
  import { Input } from "$lib/components/ui/input";
  import { Button } from "$lib/components/ui/button";
  import Check from "lucide-svelte/icons/check";
  import ChevronDown from "lucide-svelte/icons/chevron-down";
  import IconSettings from "lucide-svelte/icons/settings";
  import CircleX from "lucide-svelte/icons/circle-x";
  import { tick } from "svelte";
  import * as Command from "$lib/components/ui/command";
  import * as Popover from "$lib/components/ui/popover";
  import * as Select from "$lib/components/ui/select";
  import { cn } from "$lib/utils.js";

  const ASPNET_UPLOAD_ENDPOINT = "http://localhost:3000/upload";

  let open = $state(false);
  let value = $state("");
  let triggerRef = $state<HTMLButtonElement>(null!);
  let selectedFormat = $state("MP4");
  let selectedCodec = $state("H264");

  const fileSources = [ {value: "device", label: "From Device"}, {value: "cloud", label: "From Cloud"} ];
  const formats = [ {value: "MP4", label: "MP4"}, {value: "MKV", label: "MKV"}, {value: "AVI", label: "AVI"}, {value: "MOV", label: "MOV"}, {value: "FLV", label: "FLV"} ];

  let metadata: { format: string; codec: string }[] = $state([]);
  let files: File[] = $state([]);

  function handleFilesChange(event: Event) {
    metadata = files.map(() => ({ format: selectedFormat, codec: selectedCodec}));
    files = Array.from((event.target as HTMLInputElement).files || []);
  }

  async function handleSubmit() {
    const formData = new FormData();
    files.forEach((file, index) => {
      formData.append(`file${index + 1}`, file);
      formData.append(`format${index + 1}`, metadata[index].format);
      formData.append(`codec${index + 1}`, metadata[index].codec);
    });

    await fetch(ASPNET_UPLOAD_ENDPOINT, {
      method: "POST",
      body: formData,
    });
  }

  const selectedValue = $derived(
    fileSources.find((f) => f.value === value)?.label
  );
 
  // refocus trigger button when user selects
  // item to allow keyboard navigation
  function closeAndFocusTrigger() {
    open = false;
    tick().then(() => {
      triggerRef.focus();
    });
  
    if (value === "device") {
      const input = document.createElement("input");
      input.type = "file";
      input.multiple = true;
      input.click();
      input.onchange = (event) => {
        files = Array.from((event.target as HTMLInputElement).files || []);
      };
    }
  }
</script>

<div class="flex items-center justify-center h-full w-full p-24">
    {#if files.length > 0}
  <Input type="file" id="file-input" multiple class="invisible" on:change={(e) => handleFilesChange(e)} />
  <Button variant="primary" on:click={() => document.getElementById("file-input")!.click()}>
    Add More Files
  </Button>
      <div class="file-list">
    {#each files as file, index}
      <div class="file-item">
        <div class="file-info">
          <div>{file.name}</div>
          <div>{(file.size / (1024 * 1024)).toFixed(2)} MB</div>
        </div>
        <div class="file-controls">
          <div>
            <Select.Root type="single">
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
          </div>
          <Button variant="outline" size="icon" title="Settings" on:click={() => console.log("foo")}>
            <IconSettings />
          </Button>
          <Button variant="outline" size="icon" title="Remove File" on:click={() => { 
              files = files.filter((_, i) => i !== index);
              console.log(files); 
              }}>
            <CircleX/>
          </Button>
        </div>
      </div>
    {/each}
  </div>

  <div class="batch-options">
    <span>Convert All ({files.length}) to:</span>
    <Select.Root
          type="single"
          bind:value={selectedFormat}
        >
      <Select.Content>
          {#each formats as format}
            <Select.Item value={format.value} label={format.label}
              >{format.label}</Select.Item
            >
          {/each}
      </Select.Content>
    </Select.Root>
    <Button on:click={handleSubmit}>Compress Now</Button>
  </div>
  {:else}
    <Popover.Root bind:open>
    <Popover.Trigger bind:ref={triggerRef}>
      {#snippet child({ props })}
        <Button
          variant="outline"
          class="w-[200px] justify-between"
          {...props}
          role="combobox"
          aria-expanded={open}
        >
          {selectedValue || "Choose Files"}
          <ChevronDown class="ml-2 size-4 shrink-0 opacity-50" />
        </Button>
      {/snippet}
    </Popover.Trigger>
    <Popover.Content class="w-[200px] p-0">
      <Command.Root>
        <Command.List>
          <Command.Group>
            {#each fileSources as fileSource}
              <Command.Item
                value={fileSource.value}
                onSelect={() => {
                  value = fileSource.value;
                  closeAndFocusTrigger();
                }}
              >
                <Check
                  class={cn(
                    "mr-2 size-4",
                    value !== fileSource.value && "text-transparent"
                  )}
                />
                {fileSource.label}
              </Command.Item>
            {/each}
          </Command.Group>
        </Command.List>
      </Command.Root>
    </Popover.Content>
  </Popover.Root>
  {/if}
</div>
