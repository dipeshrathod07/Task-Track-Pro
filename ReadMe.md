# TaskTrackPro - Advanced Task Management System

## Project Overview
TaskTrackPro is an advanced task management system that facilitates seamless collaboration between Admins and Users. The system enables task creation, assignment, tracking, and status updates. It leverages PostgreSQL for data storage, Redis for real-time notifications and caching, RabbitMQ for real-time messaging, and Elasticsearch for fast, full-text search functionality. Role-based authentication ensures that Admins can manage users and monitor notifications, while Users can focus on managing their own tasks.

## Features

### User Management
- **Role-based Authentication:** Secure login and signup for both Admins and Users, including email integration.
- **User Registration & Profile Management:** Admins can view and manage all registered users.
- **Admin Notifications:** Receive alerts on new user registrations and user actions such as task CRUD operations.

### Task Management
- **CRUD Operations:** Users can create, read, update, and delete tasks.
- **Task Assignment:** Assign tasks to specific users.
- **Status Updates:** Easily update task statuses (Pending, In Progress, Completed).

### Notifications & Real-Time Communication
- **Real-Time Notifications:** Utilize Redis to display notifications for both Admins and Users.
- **Unread Notification Count:** A bell icon displays the count of unread notifications.
- **Real-Time Messaging:** RabbitMQ facilitates messaging between Admins and Users, ensuring immediate updates.

### Search and Filtering
- **Full-Text Search:** Implemented using Elasticsearch, allowing quick search on task title, description, and status.
- **Advanced Filters:** Filter tasks based on status, user, or due date for more refined results.

## Technology Stack
- **Back-End:** ASP.NET Core MVC
- **Front-End:** HTML5, CSS3, JavaScript, jQuery/AJAX, Bootstrap
- **Database:** PostgreSQL (Direct SQL Queries)
- **Cache:** Redis (for caching and notifications)
- **Message Queue:** RabbitMQ (for real-time messaging)
- **Search Engine:** Elasticsearch (for task search functionality)
- **IDE:** Visual Studio Code
- **Version Control:** TFS via Azure DevOps

## Sprint-Wise Plan

### Sprint 1: User Authentication and Role Management
- Implement user registration and login (Admin/User roles).
- Set up PostgreSQL for user data with password hashing.
- Enable Admin views for user management and notifications on new user registrations.

### Sprint 2: Task Management
- Develop CRUD operations for tasks.
- Implement task assignment and status updates.
- Integrate Redis for real-time notifications regarding task actions.
- Set up RabbitMQ for messaging on task creation, updates, and deletions.

### Sprint 3: Real-Time Notifications and Search
- Display unread notification counts using a bell icon.
- Enable real-time task updates for Admins via RabbitMQ.
- Integrate Elasticsearch for fast, full-text task search.
- Add advanced filters for tasks based on various criteria.

