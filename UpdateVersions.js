const fs = require('fs');
const resolve = require('path').resolve;
const relative = require('path').relative;
const util = require('util');
const args = process.argv.slice(2);
const conf = args[0] || 'Debug';

const SolutionDir = resolve(__dirname);
const EFIngresProviderDir = resolve(SolutionDir, 'EFIngresProvider');
const EFIngresProviderDeployDir = resolve(SolutionDir, 'EFIngresProviderDeploy');

const version = readVersion();

console.log(`Updating version to ${version}`);

updateVersionInfo(version);
updateEFIngresProviderVersion(version);
updateVSIXManifestVersion(version);
updateNuspecVersion(version);

function readVersion() {
    const path = resolve(SolutionDir, 'Version.txt');
    const versionTxt = fs.readFileSync(path, 'utf8');
    const match = /^\s*(\d+\.\d+\.\d+)\s*$/.exec(versionTxt);
    if (!match) {
        throw new Error(`Could not read version from ${path}`);
    }
    return match[1];
}

function updateVersionInfo(version) {
    const path = resolve(SolutionDir, 'VersionInfo.cs');
    const oldContents = fs.readFileSync(path, 'utf8');
    const assemblyVersion = conf === 'Release' ? `${version}` : `${version}.*`;
    const newContents = oldContents
        .replace(/\[assembly: AssemblyVersion\("[^"]*"\)\]/, `[assembly: AssemblyVersion("${assemblyVersion}")]`)
        .replace(/\[assembly: AssemblyFileVersion\("[^"]*"\)\]/, `[assembly: AssemblyFileVersion("${version}")]`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateEFIngresProviderVersion(version) {
    const path = resolve(SolutionDir, 'EFIngresProviderVSIX/EFIngresProviderVersion.cs');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/public const string Version = "[^"]*";/, `public const string Version = "${version}";`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateVSIXManifestVersion(version) {
    const path = resolve(SolutionDir, 'EFIngresProviderVSIX/source.extension.vsixmanifest');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/<Identity([^>]*)Version="[^"]*"/, `<Identity$1Version="${version}"`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateNuspecVersion(version) {
    const path = resolve(EFIngresProviderDir, 'EFIngresProvider.nuspec');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents.replace(/<version>[^<]*<\/version>/, `<version>${version}</version>`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateFileIfChanged(path, oldContents, newContents) {
    if (newContents !== oldContents) {
        fs.writeFileSync(path, newContents, 'utf8');
        console.log(`Updated ${relative(SolutionDir, path).replace(/\\/g, '/')}`);
    } else {
        console.log(`No changes for ${relative(SolutionDir, path).replace(/\\/g, '/')}`);
    }
}