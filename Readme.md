# AWS Serverless Task Manager API

This project is a personal development portfolio piece. It is a simple, serverless RESTful API for managing to-do tasks, demonstrating my ability to build and deploy a modern cloud-native application.

## Technologies and Concepts

* **C# / .NET 8:** The core language and framework for the API logic.
* **AWS Lambda:** The serverless compute service that runs the API functions. To showcase an understanding of function-as-a-service (FaaS) and event-driven architecture.
* **Amazon API Gateway:** Used to create and manage the RESTful API endpoints, acting as a secure "front door" for the Lambda function.
* **Amazon DynamoDB:** A fully managed NoSQL database used for data persistence. This demonstrates a working knowledge of non-relational databases and data modeling.
* **AWS IAM:** Used to manage secure access and permissions (following the principle of least privilege) for the Lambda function.
* **AWS CLI / .NET Lambda Tools:** Command-line tools used for local development, configuration, and deployment.
* **Git & GitHub:** Version control and source code management. The project demonstrates a clean commit history and proper use of a repository.
* **Clean Architecture:** The codebase is structured with a separation of concerns to ensure the application is maintainable, testable, and scalable.

## API Endpoints

The following endpoints are currently implemented:

* **`GET /tasks`**
    * **Purpose:** Retrieves a list of all tasks.
* **`POST /tasks`**
    * **Purpose:** Creates a new task.
* **`GET /tasks/{id}`**
    * **Purpose:** Retrieves a single task.

## How to Run

1.  **Prerequisites:** Ensure you have the .NET 8 SDK and AWS CLI installed and configured.
2.  **Clone the Repository:**
    ```bash
    git clone git@github.com:<your-username>/aws-task-manager-api.git
    cd aws-task-manager-api
    ```
3.  **Deploy to AWS:**
    ```bash
    dotnet lambda deploy-function --aws-profile <your-personal-profile-name>
    ```
4.  **Configure API Gateway:** Follow the instructions in the AWS Management Console to create a REST API and link the `/tasks` resource to your Lambda function.

## What's Next

* Implementing the remaining CRUD operations: `PUT /tasks/{id}`, and `DELETE /tasks/{id}`.
* Refactoring the project into separate layers to adhere to Clean Architecture principles.
* Adding structured logging with AWS CloudWatch.
