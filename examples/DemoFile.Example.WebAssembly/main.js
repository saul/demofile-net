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
	input.onchange = _ => {
		let files = Array.from(input.files);
		console.log(files);
		readFile(files[0]);
	};
	input.click();

	input.remove();
}

function readFile(file) {
	setDemoParseResult("");
	setDemoParseProgress(0);
	var reader = new FileReader ();
	reader.onload = async function (e) {
		var arrayBuf = reader.result;
		await demoFileProgram.ParseToEnd(new Uint8Array(arrayBuf));
	}
	reader.readAsArrayBuffer(file);
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
