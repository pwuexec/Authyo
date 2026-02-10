# Product Capabilities

This document outlines the current and planned capabilities of the Authy Identity and Access Management (IAM) system. It is intended for business stakeholders to understand the feature set and roadmap.

## Implemented Capabilities

The following features are currently implemented and available in the API.

### Authentication & Session Management

Core features for identifying users and managing their access sessions.

*   **User Login**
    Authenticates a user and issues a JWT access token and a refresh token.
    *   **Endpoint:** `POST /login`
    *   **Input:** User ID (Simplified for development; see *Planned Capabilities* for secure auth).

*   **Token Refresh**
    Refreshes an expired access token using a valid refresh token, allowing users to stay logged in without re-entering credentials.
    *   **Endpoint:** `POST /refresh`
    *   **Input:** Expired Access Token, Refresh Token.

*   **View Active Sessions**
    Retrieves a list of all active sessions (refresh tokens) for a specific user, useful for security auditing.
    *   **Endpoint:** `GET /users/{userId}/sessions`

*   **Revoke Session**
    Invalidates a specific session, effectively logging the user out of that session.
    *   **Endpoint:** `DELETE /sessions/{id}`

### Organization Management

Features for managing multi-tenant organizations.

*   **Create Organization**
    Creates a new organization within the system.
    *   **Endpoint:** `POST /organization`
    *   **Input:** Organization Name.

*   **List Organizations**
    Retrieves a list of all organizations in the system.
    *   **Endpoint:** `GET /organization`

### Role-Based Access Control (RBAC)

Features for defining roles and permissions within organizations.

*   **Manage Roles**
    Creates a new role or updates an existing one within an organization. Roles are collections of scopes (permissions).
    *   **Endpoint:** `PUT /organization/{id}/role`
    *   **Input:** Role Name, List of Scope Names.

*   **List Roles**
    Retrieves all roles defined for a specific organization.
    *   **Endpoint:** `GET /organization/{id}/role`

*   **Manage Scopes**
    Creates a new permission scope or updates an existing one within an organization.
    *   **Endpoint:** `PUT /organization/{id}/scope`
    *   **Input:** Scope Name.

*   **List Scopes**
    Retrieves all permission scopes defined for a specific organization.
    *   **Endpoint:** `GET /organization/{id}/scope`

### Ownership & Administrative Permissions

The platform distinguishes between organization owners and platform owners (root users).

*   **Organization Owners**
    Users assigned as owners of a specific organization can manage that organization's access model and users.
    *   **Role/Scope Administration:** Create/update/list roles and scopes for their organization.
    *   **User Administration:** Manage users only within their organization.
    *   **User Session Administration:** View and revoke sessions for users within their organization (and their own sessions).

*   **Platform Owner (Root User)**
    The platform owner is identified by an allowed root IP address and has cross-tenant administrative privileges.
    *   **Organization Administration:** Create and list organizations across the platform.
    *   **User Administration:** Manage users in any organization.
    *   **Cross-Organization Administration:** Manage roles/scopes and user sessions for any organization.

---

## Planned Capabilities (Roadmap)

The following features are identified as essential next steps to complete the IAM system.

### 1. Secure Authentication & User Lifecycle
*   **Secure Login**: Replace the current ID-based login with a secure username/password authentication flow (hashing, salting) or OAuth integration.
*   **User Registration**: Allow new users to sign up and create accounts.
*   **User Profile Management**: Endpoints to update user details (name, email, password).
*   **User Deletion**: Ability to remove users from the system in compliance with data privacy regulations.

### 2. Advanced Organization Management
*   **Organization Updates**: Ability to rename organizations or update their settings.
*   **Organization Deletion**: Ability to remove an organization and clean up its resources.
*   **Membership Management**:
    *   **Invite/Add Users**: Add existing users to an organization.
    *   **Remove Users**: Remove users from an organization.
    *   **Ownership**: Explicitly assign and transfer organization ownership.

### 3. Granular Access Control
*   **Role Assignment**: Assign specific roles to users within an organization (linking Users to Roles).
*   **Resource Cleanup**:
    *   **Delete Role**: Remove unused roles.
    *   **Delete Scope**: Remove unused scopes.
