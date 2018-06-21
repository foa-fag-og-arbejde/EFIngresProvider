const fs = require('fs');
const resolve = require('path').resolve;
const relative = require('path').relative;
const args = process.argv.slice(2);

const SolutionDir = resolve(__dirname);
const EFIngresProviderTestsDir = resolve(SolutionDir, 'EFIngresProvider.Tests');

if (args[0] === 'reset') {
    resetTestModelEdmx('efingres');
} else {
    const testConnection = readTestConnection();
    updateTestModelEdmx(testConnection.schema);
}

function readTestConnection() {
    const path = resolve(EFIngresProviderTestsDir, 'TestConnection.json');
    try {
        const testConnectionJson = fs.readFileSync(path, 'utf8');
        const testConnection = JSON.parse(fs.readFileSync(path, 'utf8'));
        if (!testConnection.connectionString) {
            throw new Error(`To build EFIngresProvider.Tests the file ${fmtPath(path)} must contain a valid JSON with property "connectionString"`);
        }

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
