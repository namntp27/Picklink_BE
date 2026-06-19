# Picklink Backend Plan

## 1. Muc tieu

Xay dung backend ASP.NET Core Web API cho Picklink: ung dung dat san, booking, tim doi thu va social/community. Backend dung Code First voi Entity Framework Core, SQL Server, JWT authentication, refresh token, social login Google/Facebook, phan quyen theo role va API du cho frontend React hien tai.

Frontend hien tai nam tai `D:\SEP490pass\Picklink_Web` va backend se code rieng tai `D:\SEP490pass\Picklink_BE`. Backend folder hien dang rong, se scaffold solution ASP.NET Core trong folder nay:

```text
D:\SEP490pass\Picklink_BE
  Picklink.Api/
  Picklink.Application/
  Picklink.Domain/
  Picklink.Infrastructure/
  Picklink.Tests/
  Picklink.sln
```

## 2. De xuat cong nghe

- Target mac dinh: `.NET 8 LTS` vi may hien tai da co SDK `8.0.422` va phu hop de code/build ngay.
- Huong nang cap: `.NET 10 LTS` khi cai SDK moi, vi .NET 10 la LTS moi hon va duoc Microsoft support den `2028-11-14`.
- Database: SQL Server.
- Database name: `PicklinkDb`.
- ORM: Entity Framework Core Code First.
- API docs: Swagger/OpenAPI.
- Auth: ASP.NET Core Identity + JWT + refresh token.
- Social login: Google/Facebook OAuth.
- Validation: FluentValidation.
- Mapping: AutoMapper hoac Mapster. De xuat Mapster vi nhe va it boilerplate.
- Logging: Serilog.
- Tests: xUnit + FluentAssertions + WebApplicationFactory.
- File upload: local storage truoc, de interface mo rong sang S3/Azure Blob/Cloudinary sau.

Tai lieu support .NET tham khao: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core

## 3. Kien truc

Dung Clean Architecture chia tang:

```text
Picklink.Domain
  Entities
  Enums
  Common

Picklink.Application
  DTOs
  Services
  Interfaces
  Validators
  Common

Picklink.Infrastructure
  Data
  Identity
  Repositories
  Storage
  ExternalAuth
  Email

Picklink.Api
  Controllers
  Middleware
  Filters
  Extensions
```

Nguyen tac:

- Controller mong, xu ly request/response.
- Service chua business logic.
- Repository chi dong vai tro data access khi can query phuc tap; cac CRUD don gian co the dung DbContext trong service.
- DTO rieng cho request/response, khong expose entity truc tiep.
- Global exception middleware tra loi loi dong nhat.
- Pagination/search/filter/sort dung chung query model.

## 4. Response format

De xuat response wrapper dong nhat:

```json
{
  "success": true,
  "message": "Success",
  "data": {},
  "errors": null,
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 100,
    "totalPages": 5
  }
}
```

Loi validation:

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": {
    "email": ["Email is required"]
  }
}
```

## 5. Roles va authorization

Roles:

- `Admin`: quan tri toan he thong.
- `Owner`: chu san, quan ly cum san/san, lich, booking, doanh thu.
- `Player`: nguoi choi, dat san, tim doi thu, social, club, tournament.
- `Guest`: nguoi chua dang nhap. De xuat khong luu role nay trong DB; day la anonymous user.

Thiet ke user:

- Chi co 1 entity user chinh la `User`, map xuong bang `Users`.
- `Admin`, `Owner`, `Player` la role gan cho user, khong nen tao 3 bang dang nhap rieng vi se bi trung email/password/token.
- Neu tung role can thong tin nghiep vu rieng thi tao bang 1-1 dat ten dung role:
  - `Player` cho nguoi choi.
  - `Owner` cho chu san.
  - `Admin` cho quan tri vien/noi bo.
- `Guest` la anonymous user chua dang nhap, khong tao record rieng trong DB.

Policy de xuat:

- Public: xem danh sach san, chi tiet san, bai post public, giai dau public.
- Player: dat san, thanh toan, tim doi thu, tao post, like/comment, tham gia club/giai dau.
- Owner: CRUD san cua minh, quan ly booking cua san minh, xem doanh thu.
- Admin: quan ly users, courts, bookings, posts, reports, transactions, settings.

## 6. Entity de xuat

### Identity

- `User`
  - Id, FullName, Email, PhoneNumber, AvatarUrl, DateOfBirth, Gender, City, District, Ward, Status
  - CreatedAt, UpdatedAt, DeletedAt, IsDeleted
  - Day la entity user chinh cua he thong. Moi tai khoan Admin/Owner/Player deu la 1 `User` + role.
- `Role`
  - Id, Name, Description
- `UserRole`
  - UserId, RoleId
- `Player`
  - UserId, SkillLevel, PreferredSports, Bio, TotalBookings, TotalMatches
  - 1-1 voi `User`, chi tao khi user co role `Player`.
- `Owner`
  - UserId, BusinessName, TaxCode, VerificationStatus, BankAccountName, BankAccountNumber, BankName
  - 1-1 voi `User`, chi tao khi user co role `Owner`.
- `Admin`
  - UserId, Department, PermissionNote
  - 1-1 voi `User`, chi tao khi can luu thong tin noi bo cho role `Admin`.
- `RefreshToken`
  - UserId, TokenHash, ExpiresAt, RevokedAt, DeviceInfo, IpAddress
- `ExternalLogin`
  - UserId, Provider, ProviderUserId, Email

### San va booking

- `Sport`
  - Name, Slug
- `Venue`
  - OwnerId, Name, Description, Address, City, District, Ward, Latitude, Longitude, Status
- `Court`
  - VenueId, SportId, Name, Description, PricePerHour, Status
- `CourtImage`
  - CourtId, Url, SortOrder
- `CourtFeature`
  - CourtId, Name
- `CourtSchedule`
  - CourtId, DayOfWeek, OpenTime, CloseTime
- `CourtBlockedSlot`
  - CourtId, StartTime, EndTime, Reason
- `Booking`
  - PlayerId, CourtId, StartTime, EndTime, TotalAmount, Status, Note, CancelReason
- `BookingParticipant`
  - BookingId, UserId, Status
- `PaymentTransaction`
  - BookingId, UserId, Amount, Provider, ProviderTransactionId, Status, PaidAt
- `Review`
  - BookingId, CourtId, UserId, Rating, Comment, Status

### Tim doi thu

- `MatchRequest`
  - CreatorId, SportId, VenueId, CourtId, StartTime, EndTime, SkillLevel, NeededPlayers, Visibility, Status
- `MatchParticipant`
  - MatchRequestId, UserId, Status
- `MatchInvite`
  - MatchRequestId, SenderId, ReceiverId, Status, Message

### Social/community

- `Post`
  - AuthorId, Content, Visibility, Status
- `PostMedia`
  - PostId, Url, MediaType, SortOrder
- `PostComment`
  - PostId, AuthorId, ParentCommentId, Content, Status
- `PostReaction`
  - PostId, UserId, Type
- `SavedPost`
  - PostId, UserId
- `Report`
  - ReporterId, TargetType, TargetId, Reason, Status

### Clubs

- `Club`
  - OwnerId, Name, Description, Location, ImageUrl, Visibility, Status
- `ClubMember`
  - ClubId, UserId, Role, Status
- `ClubPost`
  - ClubId, PostId

### Tournament

- `Tournament`
  - OrganizerId, SportId, Name, Description, VenueId, StartDate, EndDate, MaxParticipants, Fee, Prize, Status
- `TournamentParticipant`
  - TournamentId, UserId, Status

### Messaging va notification

- `Conversation`
  - Type, Title
- `ConversationMember`
  - ConversationId, UserId, LastReadAt
- `Message`
  - ConversationId, SenderId, Content, MessageType, AttachmentUrl
- `Notification`
  - UserId, Type, Title, Body, DataJson, ReadAt

### Audit chung

Gan cho hau het entity:

- `Id`: Guid
- `CreatedAt`
- `UpdatedAt`
- `DeletedAt`
- `IsDeleted`

## 7. Quan he chinh

- User Owner 1-n Venue.
- User 1-1 Player/Owner/Admin tuy theo role.
- Venue 1-n Court.
- Court n-1 Sport.
- Court 1-n Booking.
- User Player 1-n Booking.
- Booking 1-n BookingParticipant.
- Booking 1-1/1-n PaymentTransaction.
- Booking 1-1 Review.
- User 1-n Post, Comment, Reaction, SavedPost.
- Post 1-n Comment, Media, Reaction.
- User 1-n MatchRequest.
- MatchRequest 1-n MatchParticipant va MatchInvite.
- Club n-n User qua ClubMember.
- Tournament n-n User qua TournamentParticipant.
- Conversation n-n User qua ConversationMember.

## 8. API modules de xuat

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `GET /api/auth/me`
- `POST /api/auth/google`
- `POST /api/auth/facebook`

### Courts va venues

- `GET /api/venues`
- `GET /api/venues/{id}`
- `POST /api/owner/venues`
- `PUT /api/owner/venues/{id}`
- `GET /api/courts`
- `GET /api/courts/{id}`
- `GET /api/courts/{id}/schedule`
- `POST /api/owner/courts`
- `PUT /api/owner/courts/{id}`
- `DELETE /api/owner/courts/{id}`
- `POST /api/owner/courts/{id}/blocked-slots`

### Booking

- `POST /api/bookings`
- `GET /api/bookings/my`
- `GET /api/bookings/{id}`
- `POST /api/bookings/{id}/cancel`
- `GET /api/owner/bookings`
- `PATCH /api/owner/bookings/{id}/status`
- `GET /api/admin/bookings`

### Payment

- `POST /api/payments/create`
- `POST /api/payments/webhook`
- `GET /api/transactions/my`
- `GET /api/owner/revenue`
- `GET /api/admin/transactions`

Provider thanh toan co the de stub truoc, sau do tich hop VNPAY/MoMo/Stripe tuy nhu cau.

### Tim doi thu

- `GET /api/match-requests`
- `POST /api/match-requests`
- `GET /api/match-requests/my`
- `GET /api/match-requests/{id}`
- `POST /api/match-requests/{id}/join`
- `POST /api/match-requests/{id}/invite`
- `PATCH /api/match-invites/{id}/status`

### Social

- `GET /api/posts`
- `POST /api/posts`
- `GET /api/posts/{id}`
- `PUT /api/posts/{id}`
- `DELETE /api/posts/{id}`
- `POST /api/posts/{id}/comments`
- `POST /api/posts/{id}/reactions`
- `POST /api/posts/{id}/save`
- `DELETE /api/posts/{id}/save`
- `POST /api/reports`
- `GET /api/admin/posts`
- `PATCH /api/admin/posts/{id}/status`

### Clubs

- `GET /api/clubs`
- `POST /api/clubs`
- `GET /api/clubs/{id}`
- `POST /api/clubs/{id}/join`
- `GET /api/clubs/{id}/members`
- `PATCH /api/clubs/{id}/members/{userId}/status`

### Tournament

- `GET /api/tournaments`
- `POST /api/tournaments`
- `GET /api/tournaments/{id}`
- `POST /api/tournaments/{id}/join`
- `GET /api/tournaments/my`
- `GET /api/admin/tournaments`

### Messages va notifications

- `GET /api/conversations`
- `POST /api/conversations`
- `GET /api/conversations/{id}/messages`
- `POST /api/conversations/{id}/messages`
- `GET /api/notifications`
- `PATCH /api/notifications/{id}/read`
- `PATCH /api/notifications/read-all`

### Upload

- `POST /api/uploads/images`
- `POST /api/uploads/files`

Co gioi han file size, content type allowlist, va tra ve URL de frontend luu vao avatar/post/court image.

## 9. Search, filter, sort, pagination

Query chung:

```text
page=1
pageSize=20
search=
sortBy=createdAt
sortDirection=desc
```

Filter rieng:

- Courts: sportId, city, district, minPrice, maxPrice, rating, date, startTime, endTime, features.
- Bookings: status, fromDate, toDate, courtId, playerId.
- Match requests: sportId, skillLevel, city, fromDate, toDate, status.
- Posts: authorId, hashtag/search, visibility, createdFrom, createdTo.
- Admin lists: status, role, reportCount, createdFrom, createdTo.

## 10. Security

- Password hash bang ASP.NET Core Identity.
- JWT access token ngan han, refresh token dai han luu hash trong DB.
- Rotate refresh token moi lan refresh.
- Revoke refresh token khi logout.
- CORS default cho frontend local:
  - `http://localhost:5173`
  - `http://localhost:3000`
  - `http://127.0.0.1:5173`
- Rate limit cho auth endpoints.
- Validate upload file extension va MIME type.
- Khong tra thong tin nhay cam trong response.
- Owner chi duoc sua/xem venue/court/booking thuoc so huu cua minh.
- Admin endpoints bat buoc role Admin.

## 11. Seed data

Seed toi thieu:

- Roles: Admin, Owner, Player.
- Admin mac dinh:
  - Email: `admin@picklink.local`
  - Password: dung bien moi truong `SEED_ADMIN_PASSWORD`, khong hardcode trong source.
- Sports: Badminton, Tennis, Football, Pickleball.
- Mot vai venue/court mau cho dev.

## 12. Migration va database workflow

Lenh du kien:

```powershell
dotnet ef migrations add InitialCreate --project Picklink.Infrastructure --startup-project Picklink.Api
dotnet ef database update --project Picklink.Infrastructure --startup-project Picklink.Api
```

Connection string local de xuat:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PicklinkDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Neu dung SQL Server Express:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=PicklinkDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## 13. Testing

Unit tests:

- Auth service.
- Booking overlap/availability logic.
- Match request join/invite logic.
- Authorization ownership checks.

Integration tests:

- Register/login/refresh token.
- Create venue/court by Owner.
- Search courts.
- Create booking va chan trung slot.
- Create post/comment/reaction.
- Admin moderation.

## 14. Thu tu trien khai

### Phase 1: Scaffold va foundation

- Tao solution va projects.
- Cai packages.
- Cau hinh EF Core, SQL Server, Identity, JWT, Swagger, CORS, Serilog.
- Tao base entity, response wrapper, pagination, exception middleware.

### Phase 2: Identity/Auth

- User, roles, refresh token.
- Register/login/refresh/logout/me.
- Seed roles/admin.
- Google/Facebook login endpoint dang khung, sau do gan client id/secret that.

### Phase 3: Courts/venues/owner

- Venue, Court, CourtImage, Feature, Schedule, BlockedSlot.
- Owner CRUD.
- Public search/detail/schedule.

### Phase 4: Booking/payment

- Booking flow.
- Kiem tra trung lich.
- Booking status.
- Stub payment transaction.
- Owner bookings va revenue.

### Phase 5: Social va tim doi thu

- Posts, comments, reactions, saved posts.
- Match requests, join, invite, pending invites.
- Notifications co ban.

### Phase 6: Clubs/tournaments/messages/admin

- Clubs, members.
- Tournaments, participants.
- Conversations/messages.
- Admin moderation va dashboard APIs.

### Phase 7: Tests, docs, polish

- Unit/integration tests.
- Swagger examples.
- README backend.
- Huong dan migration/run local.

## 15. Dieu can xac nhan truoc khi code that

Neu ban dong y voi plan nay, minh se scaffold backend theo mac dinh sau:

- Project name: `Picklink`.
- Backend folder: `Picklink.Api`, `Picklink.Application`, `Picklink.Domain`, `Picklink.Infrastructure`, `Picklink.Tests`.
- Target framework: `net8.0`.
- Database: `PicklinkDb`.
- Architecture: Clean Architecture.
- Role `Guest` la anonymous user, khong tao role trong DB.
- Payment provider chua tich hop that o lan dau; tao interface va transaction model truoc.
- Upload luu local trong `wwwroot/uploads` truoc.

Neu ban muon doi bat ky diem nao, nen doi truoc khi tao migration dau tien.
