import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const { setModuleImports, getAssemblyExports, getConfig } = 
	await dotnet.create();

setModuleImports("main.js", {
  
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const demoFileProgram = exports.DemoFile.Example.WebAssembly.Program;
const text = demoFileProgram.Greeting();
console.log(text);

await dotnet.run(); // runs Main() function


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
	document.getElementById("demo_parse_result").innerHTML = "";
	var reader = new FileReader ();
	reader.onload = async function (e) {
		var arrayBuf = reader.result;
		console.log(arrayBuf.byteLength);
        var resultStr = await demoFileProgram.ParseToEnd(new Uint8Array(arrayBuf));
		document.getElementById("demo_parse_result").innerHTML = resultStr;
	}
	reader.readAsArrayBuffer(file);
}


window.document.getElementById("OpenDemoFileButton").onclick = openDemoFile;

