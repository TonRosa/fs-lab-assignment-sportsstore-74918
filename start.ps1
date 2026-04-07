# Stop any existing processes first
Write-Host "🛑 Stopping existing processes..." -ForegroundColor Red
Get-Process | Where-Object {$_.Name -like "dotnet*"} | Stop-Process -Force -ErrorAction SilentlyContinue
docker-compose down
Start-Sleep -Seconds 3

Write-Host "🚀 Starting Platform..." -ForegroundColor Green

# Start only RabbitMQ in Docker
Write-Host "🐰 Starting RabbitMQ..." -ForegroundColor Yellow
docker-compose up rabbitmq -d

Write-Host "⏳ Waiting for RabbitMQ..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Start all .NET services
Write-Host "🌐 Starting .NET Services..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project OrderManagement.API/OrderManagement.API.csproj"
Start-Sleep -Seconds 3
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project Inventory.Service/Inventory.Service.csproj"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project Payment.Service/Payment.Service.csproj"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project Shipping.Service/Shipping.Service.csproj"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project BlazorPortal/BlazorPortal.csproj"

Write-Host "✅ All services started!" -ForegroundColor Green
Write-Host ""
Write-Host "📌 URLs:" -ForegroundColor Cyan
Write-Host "   Blazor Portal:  http://localhost:5017" -ForegroundColor White
Write-Host "   API Swagger:    http://localhost:5292/swagger" -ForegroundColor White
Write-Host "   RabbitMQ:       http://localhost:15672" -ForegroundColor White
Write-Host ""
Write-Host "📌 For React Admin run:" -ForegroundColor Cyan
Write-Host "   cd admin-dashboard && npm start" -ForegroundColor White