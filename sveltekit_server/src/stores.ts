import { writable } from "svelte/store";
import type { InputFile } from "$lib/types";

export const inputFilesStore = writable<InputFile[]>([]);
