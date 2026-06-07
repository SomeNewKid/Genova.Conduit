# **Genova.Conduit**

Genova.Conduit is a lightweight C# framework for building **local-first, agent-driven applications** that combine:

> [!WARNING]
> This codebase is part of the Genova platform and should not be considered production-ready. It is published as source for review, experimentation, and reuse within Genova-related projects.

> [!IMPORTANT]
> A fresh public clone of this repository should not be expected to restore or build without additional Genova infrastructure. Many Genova dependencies are distributed through a private authenticated NuGet feed, and the public source does not include feed credentials or a complete public package graph.

* LLM-based cognition (via OpenAI or other providers)
* Local orchestrated workflows
* Reusable short-lived pipelines and steps
* Long-running agents with persistent state
* Modular tool systems for deterministic operations

This document introduces the **five foundational concepts** that structure the framework:
**Agents**, **Tasks**, **Pipelines**, **Steps**, and **Tools**.

Each concept is designed with **cohesion**, **clear responsibility boundaries**, and **strong architectural separation**, enabling developers to compose intelligent systems that are both expressive and maintainable.

---

## 📘 **Agent**

An **Agent** is an autonomous, long-running actor that works toward a goal over time.
Agents:

* Maintain persistent internal state using `AgentState`.
* Execute work incrementally across multiple cycles (`RunAsync`).
* Interpret goals, decide what to do next, and update their own progress.
* May use LLMs for reasoning, planning, or decision-making.
* May pause while waiting for external events (e.g., file existence, user approval).
* Rely on the host’s orchestrator to schedule repeated runs.

Agents **do not** perform heavy work directly; instead, they coordinate **pipelines**, **steps**, **tools**, and **tasks** to achieve their objectives.

---

## 📘 **Task**

A **Task** (`IAgentTask`) is a unit of work managed by an agent over time.
Tasks represent **what** needs to be done — not **how** it is done.

Tasks:

* Have a lifecycle (`Pending`, `InProgress`, `Completed`, `Failed`, `WaitingOnExternalEvent`).
* Carry task-specific data in a `Data` dictionary.
* May span multiple agent cycles.
* Are stored in an `IAgentTaskStore`, allowing persistence and introspection.
* Allow agents to manage multiple responsibilities in parallel or sequence.

Tasks are data objects with state. They do not perform execution on their own; agents and pipelines do the work.

---

## 📘 **Pipeline**

A **Pipeline** (`IPipeline`) is a short-lived, synchronous workflow.
Pipelines represent *how* a piece of work should be performed.

Pipelines:

* Execute immediately in a single call to `ExecuteAsync`.
* Use a `PipelineContext` to read/write transient state.
* Typically contain one or more **Steps**.
* May invoke tools, call an LLM, transform data, or perform any synchronous operation.
* Return their results through `PipelineContext`.

A pipeline is best thought of as a structured recipe for work that completes in one execution.

---

## 📘 **Step**

A **Step** (`IPipelineStep`) is a small, composable unit of execution within a pipeline.

Steps:

* Perform one focused, synchronous operation.
* Read from and write to `PipelineContext`.
* Are reusable across pipelines.
* Often wrap atomic behaviors such as:

  * Preparing a model prompt
  * Calling an LLM
  * Parsing results
  * Invoking a tool
  * Validating intermediate data

Steps are the building blocks of pipelines. A pipeline may contain multiple steps that execute in sequence.

---

## 📘 **Tool**

A **Tool** (`ITool`) is a named, atomic capability that an LLM or agent may request during execution.

Tools:

* Perform a specific deterministic action.
* Accept structured arguments (`IDictionary<string, object?>`).
* Return an object representing the outcome.
* Are registered with an `IToolRegistry`.
* Can represent both local and remote operations:

  * Math operations
  * File access
  * Database or API calls
  * Business logic functions

Tools are the mechanism by which LLM reasoning can translate into concrete actions performed by the system.

---

## 📘 **Putting It All Together**

In Genova.Conduit:

* **Tools** perform atomic capabilities.
* **Steps** use tools and other logic within a pipeline.
* **Pipelines** process work synchronously using steps and a `PipelineContext`.
* **Tasks** represent long-lived units of work that agents manage across cycles.
* **Agents** orchestrate everything:

  * reading/writing persistent state,
  * choosing what to do next,
  * and invoking pipelines, tasks, or tools when needed.

This layered model allows developers to build intelligent, flexible systems that combine **LLM cognition** with **local execution**, while keeping responsibilities focused and components reusable.

## Third-Party Notices

This project has direct runtime dependencies on third-party NuGet packages, including `Microsoft.Extensions.*` packages (MIT), `Microsoft.ML*` packages (MIT). See each package's NuGet license metadata for full license and notice terms.

## License

GNU General Public License v3.0. See the `LICENSE` file for details.
