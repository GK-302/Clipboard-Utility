[English](CONTRIBUTING.md) | [日本語](japanese/CONTRIBUTING.ja.md)
***
# Contributing to Clipboard Utility

Thank you for visiting the project. We welcome your contributions.

## Community

We don't have any community outside of GitHub yet.

## Prerequisites

  * **OS**: Windows 10 / 11
  * **.NET SDK**: .NET 8.0
  * **Visual Studio 2022**:
      * In the installer, please select the **".NET desktop development"** workload.

## Setting up the Environment

### Method 1: Using Visual Studio (Recommended)

0.  (If you plan to make changes) Fork the repository
1.  Clone the repository
    ```
    git clone <URL of the repository to clone>
    ```
2.  (If not done automatically) Restore NuGet packages from the Solution Explorer
3.  Run

### Method 2: Using the Command Line

0.  (If you plan to make changes) Fork the repository
1.  Clone the repository
    ```
    git clone <URL of the repository to clone>
    cd <path to the cloned folder>
    ```
2.  Restore dependencies
    ```
    dotnet restore
    ```
3.  Build
    ```
    dotnet build
    ```
4.  Run
    ```
    dotnet run --project <project to run (.csproj)>
    ```

#### If you get a "The project failed to load" error

Launch the Visual Studio Installer and make sure that the ".NET desktop development" workload is installed correctly.

## How to Contribute

1.  Fork the repository
2.  Clone the repository
3.  Create a branch (for code or documentation changes)
4.  Test your changes
5.  Create a pull request

## Code of Conduct

[English](CODE_OF_CONDUCT.md) | [日本語](japanese/CODE_OF_CONDUCT.ja.md)