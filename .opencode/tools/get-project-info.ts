import { tool } from "@opencode-ai/plugin";
import { readFileSync } from "fs";

export default tool({
  description: "Obtiene informacion del proyecto desde package.json",
  args: {},
  async execute() {
    const pkg = JSON.parse(readFileSync("package.json", "utf-8"));
    return `Nombre: ${pkg.name}, Version: ${pkg.version}`;
  }
});
