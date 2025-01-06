# 1. Lan-SocialMedia-API

ASP.NET Project - Social Media REST API

## 1.1. **Concepts**

### 1.1.1. Roles and Actions

| No  | Owner                   | Viewer             |
| --- | ----------------------- | ------------------ |
| 1   | Manage Profile          | View Profile       |
| 2   | Create Posts            | View Posts         |
| 3   | Manage Posts            | Hide Posts         |
| 4   | Create Comment          | Create Comment     |
| 5   | Edit Comment            | Edit Comment       |
| 6   | Hide Comment            | Like Post          |
| 7   | Like Post               | View Event         |
| 8   | Create Event            | Request Join Event |
| 9   | Manage Event            |                    |
| 10  | Invite Event Attendance |                    |
|     |                         |                    |

## 1.2. **Database**

### 1.2.1. Table Users

|     | Name         | Type     |                                   |
| --- | ------------ | -------- | --------------------------------- |
| PK  | **Id**       | int      | Auto-increament                   |
|     | Fullname     | string   | Length(255)                       |
|     | Username     | string   | Length(64), unique                |
|     | Email        | string   | Length(128), unique               |
|     | PasswordHash | string   | Length(255)                       |
|     | CreatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.4. Table UserProfiles

|     | Name         | Type     |                                    |
| --- | ------------ | -------- | ---------------------------------- |
| PK  | **Id**       | int      | Auto-increament                    |
|     | Bio          | string   | nullable                           |
|     | ProfileImage | string   | nullable                           |
|     | PhoneNumber  | string   | Length(32), unique, nullable       |
|     | CityBorn     | string   | Length(255), nullable              |
|     | CityHome     | string   | Length(255), nullable              |
|     | BirthDate    | DateOnly | Length(10), 'yyyy-MM-dd', nullable |
| FK  | **UserId**   | int      |                                    |
|     | CreatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'  |
|     | UpdatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'  |

### 1.2.3. Table UserStatus

|     | Name          | Type     |                                                       |
| --- | ------------- | -------- | ----------------------------------------------------- |
| PK  | **Id**        | int      | Auto-increament                                       |
|     | AccountStatus | byte     | Enum(1=Unverified, 2=Verified, 3=Suspend, 4=Inactive) |
| FK  | **UserId**    | int      |                                                       |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'                     |

### 1.2.3. Table UserTokens

|     | Name        | Type     |                                   |
| --- | ----------- | -------- | --------------------------------- |
| PK  | **Id**      | int      | Auto-increament                   |
|     | Token       | string   | Unique                            |
|     | UserAgent   | string   |                                   |
|     | ExpiresDate | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
| FK  | **UserId**  | int      |                                   |
|     | CreatedAt   | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.2. Table UserVerifications

|     | Name             | Type     |                                             |
| --- | ---------------- | -------- | ------------------------------------------- |
| PK  | **Id**           | int      | Auto-increament                             |
|     | Code             | string   | Length(6)                                   |
|     | VerificationType | byte     | Enum(1=Register, 10=UsernameChanges         |
|     |                  |          | 20=PasswordChanges, 21=PasswordReset)       |
|     | PhoneNumber      | string   | Length(32), nullable                        |
|     | Email            | string   | Length(128), nullable                       |
|     | ExpiresDate      | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | VerifiedAt       | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
| FK  | **UserId**       | int      |                                             |
|     | CreatedAt        | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.2.5. Table UserSessionLogs

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | IpAddress  | string   | Length(64), nullable              |
|     | UserAgent  | string   | nullable                          |
|     | Action     | string   | nullable                          |
| FK  | **UserId** | int      | nullable                          |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.6. Table Groups

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | Name          | string   | Length(255)                       |
|     | Slug          | string   | Length(255), unique               |
|     | ProfileImage  | string   | nullable                          |
|     | Description   | string   | Length(1024), nullable            |
| FK  | **CreatorId** | int      |                                   |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.7. Table GroupMembers

|     | Name         | Type     |                                                 |
| --- | ------------ | -------- | ----------------------------------------------- |
| PK  | **Id**       | int      | Auto-increament                                 |
|     | Status       | byte     | Enum(0=Request, 1=Rejected, 2=Approved, 3=Left) |
| FK  | **GroupId**  | int      |                                                 |
| FK  | **MemberId** | int      |                                                 |
|     | CreatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'               |
|     | UpdatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'               |

### 1.2.8. Table UserPosts

|     | Name                | Type     |                                   |
| --- | ------------------- | -------- | --------------------------------- |
| PK  | **Id**              | int      | Auto-increament                   |
|     | Slug                | string   | Length(255), unique               |
|     | CommentAvailability | bool     | default(true)                     |
|     | Content             | string   | nullable                          |
|     | Media               | string[] | nullable                          |
| FK  | **AuthorId**        | int      |                                   |
| FK  | **GroupId**         | int      | nullable                          |
|     | CreatedAt           | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt           | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.9. Table PostComments

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | Content       | string   |                                   |
| FK  | **UserId**    | int      |                                   |
| FK  | **PostId**    | int      |                                   |
| FK  | **CommentId** | int      | nullable                          |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.10. Table PostReactions

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | Type       | byte     |                                   |
| FK  | **PostId** | int      |                                   |
| FK  | **UserId** | int      |                                   |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.10. Table CommentReactions

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | Type          | byte     |                                   |
| FK  | **CommentId** | int      |                                   |
| FK  | **UserId**    | int      |                                   |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.10. Table PostReactions

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | Type       | byte     |                                   |
| FK  | **PostId** | int      |                                   |
| FK  | **UserId** | int      |                                   |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.2.11. Table UserEvents

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | Name          | string   | Length(255)                       |
|     | Slug          | string   | Length(255), unique               |
|     | Description   | string   | Length(2048), nullable            |
|     | Location      | string   | Length(255)                       |
|     | StartTime     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | EndTime       | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
| FK  | **CreatorId** | int      |                                   |
| FK  | **GroupId**   | int      | nullable                          |
| FK  | **PostId**    | int      | nullable                          |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
