using Microsoft.Maui.Controls;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TraceMe;

public partial class MainPage : ContentPage
{
    private readonly IBluetoothLE _bluetoothLE;
    private readonly IAdapter _adapter;
    private IDevice? _device;
    private ICharacteristic? _characteristic;
    private CancellationTokenSource? _scanCancellationToken;
    private static readonly Guid ServiceUuid = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    private static readonly Guid CharacteristicUuid = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
    public MainPage()
    {
        InitializeComponent();
        Shell.SetBackgroundColor(this, Colors.Black);
        Shell.SetForegroundColor(this, Colors.White);
        Shell.SetTitleColor(this, Colors.White);
        _bluetoothLE = CrossBluetoothLE.Current;
        _adapter = _bluetoothLE.Adapter;
        GetDataButton_Clicked();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_device?.State != Plugin.BLE.Abstractions.DeviceState.Connected)
        {
            await ScanAndConnectToDevice();
        }
    }
    private async Task ScanAndConnectToDevice()
    {
        try
        {
            if (!_bluetoothLE.IsAvailable || !_bluetoothLE.IsOn)
            {
                await DisplayAlert("Ошибка", "Bluetooth выключен или недоступен", "OK");
                return;
            }
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Разрешения", "Требуется разрешение на местоположение для сканирования Bluetooth", "OK");
                        return;
                    }
                }
            }
            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
            _scanCancellationToken?.Cancel();
            _scanCancellationToken = new CancellationTokenSource();
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            await _adapter.StartScanningForDevicesAsync();
            await Task.Delay(10_000, _scanCancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
           
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Ошибка", $"Сканирование не удалось: {ex.Message}", "OK")
            );
        }
        finally
        {
            if (_adapter.IsScanning)
                await _adapter.StopScanningForDevicesAsync();

            _adapter.DeviceDiscovered -= OnDeviceDiscovered;
        }
    }

    private async void OnDeviceDiscovered(object sender, DeviceEventArgs e)
    {
        if (e.Device.Name?.Equals("TraceMe Pro", StringComparison.OrdinalIgnoreCase) != true)
            return;

        _scanCancellationToken?.Cancel();
        try
        {
            if (_adapter.IsScanning)
                await _adapter.StopScanningForDevicesAsync();
            await _adapter.ConnectToDeviceAsync(e.Device);
            _device = e.Device;
            if (_device is IDevice deviceWithMtu)
            {
                try
                {
                    await deviceWithMtu.RequestMtuAsync(128);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MTU request failed: {ex.Message}");
                }
            }
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Успех", "Подключено к TraceMe Pro", "OK")
            );
            await InitializeCharacteristic();
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Ошибка", $"Подключение не удалось: {ex.Message}", "OK")
            );
        }
    }
    private async Task InitializeCharacteristic()
    {
        if (_device == null) return;
        var services = await _device.GetServicesAsync();
        var service = services.FirstOrDefault(s => s.Id == ServiceUuid);
        if (service == null)
            throw new InvalidOperationException("Сервис не найден");
        var characteristics = await service.GetCharacteristicsAsync();
        _characteristic = characteristics.FirstOrDefault(c => c.Id == CharacteristicUuid);
        if (_characteristic == null)
            throw new InvalidOperationException("Характеристика не найдена");
        _characteristic.ValueUpdated += OnCharacteristicValueUpdated;
        await _characteristic.StartUpdatesAsync();
        await _characteristic.ReadAsync();
    }
    private async void OnCharacteristicValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
    {
        var text = e.Characteristic.Value?.Length > 0 ? System.Text.Encoding.UTF8.GetString(e.Characteristic.Value) : "Пусто";
        await MainThread.InvokeOnMainThreadAsync(() =>
            LastFeedback_label.Text = text
        );
    }
    private async void GetDataButton_Clicked()
    {
        if (_device?.State != Plugin.BLE.Abstractions.DeviceState.Connected)
        {
            await DisplayAlert("Ошибка", "Устройство не подключено. Сканирую...", "OK");
            await ScanAndConnectToDevice();
            return;
        }
        if (_characteristic == null)
        {
            try
            {
                await InitializeCharacteristic();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
                return;
            }
        }
        try
        {
            await _characteristic.ReadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Чтение не удалось: {ex.Message}", "OK");
        }
    }
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            if (_characteristic != null)
            {
                _characteristic.ValueUpdated -= OnCharacteristicValueUpdated;
                await _characteristic.StopUpdatesAsync();
            }
            if (_device?.State == Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                await _adapter.DisconnectDeviceAsync(_device);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка отключения: {ex}");
        }
        _scanCancellationToken?.Cancel();
        _adapter.DeviceDiscovered -= OnDeviceDiscovered;
    }
    private async void OnMenuClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
    private async void OnHomeClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//MainPage");
    private async void OnChartClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//ActivityPage");
    private async void OnSettingsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//SettingsPage");
    private async void EnterDataButton_Clicked(object sender, EventArgs e)
    {
        string data = await DisplayPromptAsync("TraceMe", "Введите контекстные данные", "OK", "Отмена");
    }
    private async void GetResponseButton_Clicked(object sender, EventArgs e)
    {
        using var client = new HttpClient();
        var content = new StringContent("Привет, сервер!", Encoding.UTF8, "text/plain");
        var response = await client.PostAsync("https://2ea3fcc81877.ngrok-free.app/data_send", content);
        string result = await response.Content.ReadAsStringAsync();
        LastAIFeedback_label.Text = result;
    }
}