const fs = require('fs');
const resolve = require('path').resolve;
const relative = require('path').relative;
const args = process.argv.slice(2);

const SolutionDir = resolve(__dirname);
const EFIngresProviderTestsDir = resolve(SolutionDir, 'EFIngresProvider.Tests');

if (args[0] === 'reset') {
    console.log(`Resetting connection information for tests`);
    //resetTestModelEdmx();
    resetTestConnectionCs();
} else {
    const testConnection = readTestConnection();
    console.log(`Updating connection information for tests`);
    updateTestModelEdmx(testConnection.schema);
    updateTestConnectionCs(testConnection.connectionString);
}

function readTestConnection() {
    const path = resolve(EFIngresProviderTestsDir, 'TestConnection.json');
    try {
        console.log(path);
        const testConnectionJson = fs.readFileSync(path, 'utf8');
        const testConnection = JSON.parse(fs.readFileSync(path, 'utf8'));
        if (!testConnection.connectionString) {
            throw new Error(`To build EFIngresProvider.Tests the file ${fmtPath(path)} must contain a valid JSON with property "connectionString"`);
        }

        console.log(testConnection);
        const properties = getConnectionStringProperties(testConnection.connectionString);
        testConnection.schema = testConnection.schema || properties['dbms_user'] || properties['user id'] || 'schema';
        return testConnection;
    } catch (err) {
        throw new Error(`To build EFIngresProvider.Tests the file ${fmtPath(path)} must contain a valid JSON with property "connectionString":\n${err}`);
    }
}

function updateTestModelEdmx(schema) {
    const path = resolve(EFIngresProviderTestsDir, 'TestModel/TestModel.edmx');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/<EntitySet([^>]*)Schema="[^"]*"/g, `<EntitySet$1Schema="${schema}"`);
    updateFileIfChanged(path, oldContents, newContents);
}

function resetTestModelEdmx() {
    const path = resolve(EFIngresProviderTestsDir, 'TestModel/TestModel.edmx');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/<EntitySet([^>]*)Schema="[^"]*"/g, `<EntitySet$1Schema="efingres"`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateTestConnectionCs(connectionString) {
    const path = resolve(EFIngresProviderTestsDir, 'TestConnection.cs');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/public const string ConnectionString = \@"[^"]*";/, `public const string ConnectionString = \@"${connectionString}";`);
    updateFileIfChanged(path, oldContents, newContents);
}

function resetTestConnectionCs() {
    const path = resolve(EFIngresProviderTestsDir, 'TestConnection.cs');
    const oldContents = fs.readFileSync(path, 'utf8');
    const newContents = oldContents
        .replace(/public const string ConnectionString = \@"[^"]*";/, `public const string ConnectionString = \@"";`);
    updateFileIfChanged(path, oldContents, newContents);
}

function updateFileIfChanged(path, oldContents, newContents) {
    if (newContents !== oldContents) {
        fs.writeFileSync(path, newContents, 'utf8');
        console.log(`Updated ${fmtPath(path)}`);
    } else {
        console.log(`No changes for ${fmtPath(path)}`);
    }
}

function getConnectionStringProperties(connectionString) {
    const properties = {};
    connectionString.split(/;/g).forEach(function (propertyStr) {
        const match = /^([^=]+)=(.*)$/.exec(propertyStr);
        if (match) {
            properties[match[1].toLowerCase()] = match[2];
        }
    });
    return properties;
}

function fmtPath(path) {
    return relative(SolutionDir, path).replace(/\\/g, '/');
}
