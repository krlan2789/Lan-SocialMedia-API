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

## 1.2. **Type Enum**

### 1.2.1. AccountStatusEnum

| Code | Label      |
| ---- | ---------- |
| 1    | Unverified |
| 2    | Verified   |
| 3    | Suspend    |
| 4    | Inactive   |
| 5    | Deleted    |
|      |            |

### 1.2.2. GroupMemberStatusEnum

| Code | Label    |
| ---- | -------- |
| 1    | Request  |
| 2    | Approved |
| 3    | Rejected |
| 4    | Left     |
| 5    | Removed  |
|      |          |

### 1.2.3. PrivacyTypeEnum

| Code | Label     |
| ---- | --------- |
| 1    | Public    |
| 2    | Protected |
| 3    | Private   |
|      |           |

### 1.2.4. ReactionTypeEnum

| Code | Label      |
| ---- | ---------- |
| 1    | Like       |
| 2    | Funny      |
| 3    | Insightful |
| 4    | Sad        |
|      |            |

### 1.2.5. VerificationTypeEnum

| Code | Label               |
| ---- | ------------------- |
| 1    | Register            |
| 10   | UsernameChanges     |
| 20   | PasswordChanges     |
| 21   | PasswordReset       |
| 40   | AccountDeactivation |
| 41   | AccountDeletion     |
|      |                     |

### 1.2.6. MediaTypeEnum

| Code | Label |
| ---- | ----- |
| 1    | Image |
| 2    | Audio |
| 3    | Video |
|      |       |

## 1.3. **Database**

### 1.3.1. Table Users

|     | Name         | Type     |                                             |
| --- | ------------ | -------- | ------------------------------------------- |
| PK  | **Id**       | int      | Auto-increament                             |
|     | Fullname     | string   | Length(255)                                 |
|     | Username     | string   | Length(64), unique                          |
|     | Email        | string   | Length(128), unique                         |
|     | PasswordHash | string   | Length(255)                                 |
|     | DeletedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
|     | CreatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | UpdatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.2. Table UserProfiles

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

### 1.3.3. Table UserStatus

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | AccountStatus | byte     | Enum(AccountStatusEnum)           |
| FK  | **UserId**    | int      |                                   |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.4. Table UserTokens

|     | Name        | Type     |                                   |
| --- | ----------- | -------- | --------------------------------- |
| PK  | **Id**      | int      | Auto-increament                   |
|     | Token       | string   | Unique                            |
|     | UserAgent   | string   |                                   |
|     | ExpiresDate | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
| FK  | **UserId**  | int      |                                   |
|     | CreatedAt   | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.5. Table UserVerifications

|     | Name             | Type     |                                             |
| --- | ---------------- | -------- | ------------------------------------------- |
| PK  | **Id**           | int      | Auto-increament                             |
|     | Code             | string   | Length(6)                                   |
|     | VerificationType | byte     | Enum(VerificationTypeEnum)                  |
|     | PhoneNumber      | string   | Length(32), nullable                        |
|     | Email            | string   | Length(128), nullable                       |
|     | ExpiresDate      | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | VerifiedAt       | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
| FK  | **UserId**       | int      |                                             |
|     | CreatedAt        | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.6. Table UserVerificationTokens

|     | Name             | Type     |                                             |
| --- | ---------------- | -------- | ------------------------------------------- |
| PK  | **Id**           | int      | Auto-increament                             |
|     | Token            | string   |                                             |
|     | VerificationType | byte     | Enum(VerificationTypeEnum)                  |
|     | Email            | string   | Length(128), nullable                       |
|     | ExpiresDate      | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | VerifiedAt       | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
| FK  | **UserId**       | int      |                                             |
|     | CreatedAt        | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.7. Table UserSessionLogs

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | IpAddress  | string   | Length(64), nullable              |
|     | UserAgent  | string   | nullable                          |
|     | Action     | string   | nullable                          |
| FK  | **UserId** | int      | nullable                          |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.8. Table Groups

|     | Name          | Type     |                                             |
| --- | ------------- | -------- | ------------------------------------------- |
| PK  | **Id**        | int      | Auto-increament                             |
|     | Name          | string   | Length(255)                                 |
|     | Slug          | string   | Length(255), unique                         |
|     | PrivacyType   | byte     | Enum(PrivacyTypeEnum)                       |
|     | ProfileImage  | string   | nullable                                    |
|     | Description   | string   | Length(1024), nullable                      |
| FK  | **CreatorId** | int      |                                             |
|     | DeletedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.9. Table GroupMembers

|     | Name         | Type     |                                             |
| --- | ------------ | -------- | ------------------------------------------- |
| PK  | **Id**       | int      | Auto-increament                             |
|     | Slug         | string   | Length(255), unique                         |
|     | Status       | byte     | Enum(GroupMemberStatusEnum)                 |
| FK  | **GroupId**  | int      |                                             |
| FK  | **MemberId** | int      |                                             |
|     | JoinedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
|     | CreatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | UpdatedAt    | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.10. Table UserPosts

|     | Name                | Type     |                                             |
| --- | ------------------- | -------- | ------------------------------------------- |
| PK  | **Id**              | int      | Auto-increament                             |
|     | Slug                | string   | Length(255), unique                         |
|     | CommentAvailability | bool     | default(true)                               |
|     | Content             | string   | nullable                                    |
| FK  | **AuthorId**        | int      |                                             |
| FK  | **GroupId**         | int      | nullable                                    |
|     | DeletedAt           | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
|     | CreatedAt           | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | UpdatedAt           | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.11. Table PostMedia

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | MediaPath  | string   |                                   |
|     | MediaType  | byte     | Enum(MediaTypeEnum)               |
| FK  | **PostId** | int      |                                   |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.12. Table Hashtags

|     | Name      | Type     |                                   |
| --- | --------- | -------- | --------------------------------- |
| PK  | **Id**    | int      | Auto-increament                   |
|     | Tag       | string   | Length(64), unique                |
|     | CreatedAt | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.13. Table PostHashtags

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
| FK  | **PostId**    | int      |                                   |
| FK  | **HashtagId** | int      |                                   |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.14. Table PostComments

|     | Name          | Type     |                                             |
| --- | ------------- | -------- | ------------------------------------------- |
| PK  | **Id**        | int      | Auto-increament                             |
|     | Content       | string   |                                             |
| FK  | **UserId**    | int      |                                             |
| FK  | **PostId**    | int      |                                             |
| FK  | **CommentId** | int      | nullable                                    |
|     | DeletedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss', nullable |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss'           |

### 1.3.15. Table PostReactions

|     | Name       | Type     |                                   |
| --- | ---------- | -------- | --------------------------------- |
| PK  | **Id**     | int      | Auto-increament                   |
|     | Type       | byte     | Enum(ReactionTypeEnum)            |
| FK  | **PostId** | int      |                                   |
| FK  | **UserId** | int      |                                   |
|     | CreatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt  | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.16. Table CommentReactions

|     | Name          | Type     |                                   |
| --- | ------------- | -------- | --------------------------------- |
| PK  | **Id**        | int      | Auto-increament                   |
|     | Type          | byte     | Enum(ReactionTypeEnum)            |
| FK  | **CommentId** | int      |                                   |
| FK  | **UserId**    | int      |                                   |
|     | CreatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |
|     | UpdatedAt     | DateTime | Length(20), 'yyyy-MM-dd HH:mm:ss' |

### 1.3.17. Table UserEvents (Not Yet Implemented)

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
