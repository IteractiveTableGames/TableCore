# AI Agent Instructions for the TableCore Repository

This document provides instructions for AI agents working on the TableCore project. Please adhere to these guidelines to ensure a smooth and efficient development process.

## Development Workflow

Follow this workflow for every task:

1.  **Select a Ticket:** Choose an issue from the "Backlog" column in the TableCore project board.
2.  **Move to Ready:** Move the selected issue to the "Ready" column.
3.  **Move to In Progress:** When you are ready to start working on the issue, move it to the "In Progress" column.
4.  **Code Changes:** Implement the required code changes as described in the issue.
5.  **Create Unit Tests:** Create comprehensive unit tests for your changes to ensure correctness and prevent regressions.
6.  **Validate Changes:** Run all unit tests to validate your changes and ensure that you haven't introduced any breaking changes.
7.  **Create a Pull Request:** Once your changes are complete and validated, create a Pull Request against the `main` branch.
8.  **Move to In Review:** After creating the Pull Request, move the corresponding issue to the "In Review" column.

## Technical Guidelines

*   **Target Platform:** The application is designed for a large-scale, multi-touch display running on Windows 11. All UI and interaction design should be optimized for this environment.
*   **Language:** All code must be written in C# using modern best practices and a clean, readable style.
*   **Framework:** The project uses the latest stable version of the Godot game engine with .NET.
*   **Architecture:** The project follows the architecture outlined in the `TableCore_Architecture_v0.2.md` document. This includes a core framework with an extensible module system. Pay close attention to the separation of concerns between the framework and the modules.
*   **Input:** The primary input method is multi-touch. All interactions should be designed with touch in mind.
*   **Display:** The main game board has a fixed orientation ("flat board"), while each player has a rotated HUD oriented towards their edge of the screen.
*   **Offline-First:** The application is designed for local, offline play.

## General Instructions

*   **Read the Documentation:** Before starting any task, thoroughly read the `TableCore_Architecture_v0.2.md` document and the details in the issue description.
*   **Code Comments:** Add clear and concise comments to your code, especially for public APIs and complex logic.
*   **Commit Messages:** Write clear and descriptive commit messages that explain the purpose of your changes.
*   **Ask for Clarification:** If you have any questions or are unsure about any aspect of a task, please ask for clarification before proceeding.
