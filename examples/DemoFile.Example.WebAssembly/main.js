import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const { setModuleImports, getAssemblyExports, getConfig } = 
	await dotnet.create();

// define JS exports (which can be imported into C#)
setModuleImports("main.js", {
  cs_setDemoParseResult: function(result){
	  setDemoParseResult(result);
  },
  cs_appendDemoParseResult: function(result){
	  appendDemoParseResult(result);
  },
  cs_setDemoParseProgress: function(progress){
	  setDemoParseProgress(progress);
  }
});

// obtain exported C# object
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const demoFileProgram = exports.DemoFile.Example.WebAssembly.Program;

// print string returned from C#
const greetingText = demoFileProgram.Greeting();
console.log(greetingText);

// setup button click
window.document.getElementById("OpenDemoFileButton").onclick = openDemoFile;


function openDemoFile()
{
	let input = document.createElement('input');
	input.type = 'file';
	input.onchange = async _ => {
		let files = Array.from(input.files);
		console.log(files);
		await readFile(files[0]);
	};
	input.click();

	input.remove();
}

async function readFile(file) {
	setDemoParseResult("");
	setDemoParseProgress(0);

	// by parsing a demo file from URL, we don't allocate memory for entire file (which can even be 400 MB),
	// but rather read file asyncly in chunks

	var url = URL.createObjectURL(file);
	await demoFileProgram.ParseToEndFromUrl(url);
	URL.revokeObjectURL(url);
}

function setDemoParseResult(result) {
	var elem = document.getElementById("demo_parse_result");
	elem.innerHTML = result;
	elem.scrollTop = elem.scrollHeight;
}

function appendDemoParseResult(result) {
	var elem = document.getElementById("demo_parse_result");
	elem.innerHTML += result;
	elem.scrollTop = elem.scrollHeight;
}

function setDemoParseProgress(progress) {
	var elem = document.getElementById("demoProcessingProgress");
	elem.value = progress;
}
