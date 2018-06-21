#!/bin/sh
git reset -- ./EFIngresProvider.Tests/TestModel/TestModel.edmx
node ./UpdateTests.js reset
git add ./EFIngresProvider.Tests/TestModel/TestModel.edmx
