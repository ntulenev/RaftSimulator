@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\RaftSimulator\RaftSimulator.csproj"
set "PEERS=1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003;4=http://localhost:5004;5=http://localhost:5005"

pushd "%ROOT%"

echo Building RaftSimulator...
dotnet build "%PROJECT%"
if errorlevel 1 (
    echo Build failed. Nodes were not started.
    popd
    exit /b 1
)

echo Starting 5 Raft nodes...

start "Raft Node 01" cmd /k dotnet run --no-build --no-launch-profile --project "%PROJECT%" -- --Raft:NodeId=1 --Raft:Port=5001 --Raft:Peers="%PEERS%"
timeout /t 1 /nobreak >nul

start "Raft Node 02" cmd /k dotnet run --no-build --no-launch-profile --project "%PROJECT%" -- --Raft:NodeId=2 --Raft:Port=5002 --Raft:Peers="%PEERS%"
timeout /t 1 /nobreak >nul

start "Raft Node 03" cmd /k dotnet run --no-build --no-launch-profile --project "%PROJECT%" -- --Raft:NodeId=3 --Raft:Port=5003 --Raft:Peers="%PEERS%"
timeout /t 1 /nobreak >nul

start "Raft Node 04" cmd /k dotnet run --no-build --no-launch-profile --project "%PROJECT%" -- --Raft:NodeId=4 --Raft:Port=5004 --Raft:Peers="%PEERS%"
timeout /t 1 /nobreak >nul

start "Raft Node 05" cmd /k dotnet run --no-build --no-launch-profile --project "%PROJECT%" -- --Raft:NodeId=5 --Raft:Port=5005 --Raft:Peers="%PEERS%"

echo Nodes are starting on ports 5001-5005.
echo Close the opened command windows or press Ctrl+C in each window to stop the simulation.

popd
endlocal
