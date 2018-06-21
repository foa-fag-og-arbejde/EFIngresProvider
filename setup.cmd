@setlocal
@cd %~dp0

:: Create pre-commit hook
copy /Y pre-commit.sh .git\hooks\pre-commit

:: Update tests
node UpdateTests.js
