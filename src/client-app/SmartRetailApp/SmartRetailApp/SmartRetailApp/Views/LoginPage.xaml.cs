﻿#define AUTH
using Microsoft.AppCenter;
using Microsoft.AppCenter.Auth;
using SmartRetailApp.Models;
using SmartRetailApp.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartRetailApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        UserInformation UserInfo { get; set; }

        public LoginPage()
        {

            InitializeComponent();

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;

            edtBoxName.Text = "SmartBox1";

#if AUTH
            btnStartShopping.IsEnabled = false;
            btnLoginLogout.IsVisible = true;
#else
            btnStartShopping.IsEnabled = true;
            btnLoginLogout.IsVisible = false;
#endif


            btnLoginLogout.Clicked += async (sender, e) =>
            {
                if (btnLoginLogout.Text == "ログアウト")
                {
                    await SignOut();
                }
                else
                {
                    await SignInAsync();
                }
            };
        }

        async Task SignOut()
        {
            Auth.SignOut();
            btnLoginLogout.Text = "ログイン";
            btnStartShopping.IsEnabled = false;

            await DisplayAlert("ログアウトしました", "", "OK");
        }

        async Task SignInAsync()
        {
            try
            {
                // Sign-in succeeded.
                this.UserInfo = await Auth.SignInAsync();
                string accountId = this.UserInfo.AccountId;
                Console.WriteLine($"id_token={UserInfo.IdToken}");

                btnLoginLogout.Text = "ログアウト";
                btnStartShopping.IsEnabled = true;

                await DisplayAlert("ログインしました", $"AccountId={accountId}", "OK");
            }
            catch (Exception e)
            {
                await DisplayAlert("ログインできませんでした", e.ToString(), "OK");
            }
        }

        protected override async void OnAppearing()
        {
#if AUTH
            await SignInAsync();
#endif
        }


        private async void LoginClicked(object sender, EventArgs e)
        {
            await ViewScanPageAsync();
        }

        /// <summary>
        /// インジケータ表示の切り替え
        /// </summary>
        /// <param name="isVisible"></param>
        void SetIndicator(bool isVisible)
        {
            if (isVisible)
            {
                loadingIndicator.VerticalOptions = LayoutOptions.CenterAndExpand;
                loadingIndicator.HeightRequest = 50;
                loadingIndicator.IsRunning = true;
                loadingIndicator.IsVisible = true;

                // 全体を隠す
                statckLayout.IsVisible = false;
            }
            else
            {
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;

                statckLayout.IsVisible = true;
            }
        }

        /// <summary>
        /// スキャンページに遷移する
        /// </summary>
        /// <returns></returns>
        private async Task ViewScanPageAsync()
        {
            try
            {
                // Box名を保存
                (Application.Current as App).BoxId = edtBoxName.Text;

                // インジケータを表示
                SetIndicator(true);

                // カメラでQRコードを撮影する
                var scanner = DependencyService.Get<IQrScanningService>();
                var scanResult = await scanner.ScanAsync();
                if (scanResult != null)
                {
                    (Application.Current as App).BoxId = scanResult.Text;
                }

                // デバイスIDを取得
                var deviceId = await AppCenter.GetInstallIdAsync();

                // 取引開始
                var api = new CartApiService();
                var cartResult = await api.CartStartAsync(new CartStart
                {
                    BoxId = (Application.Current as App).BoxId,
                    DeviceId = deviceId.ToString()
                });

                // 取引開始で商品カートへ遷移
                if (cartResult != null && /*cartResult.IsSuccess*/ !string.IsNullOrEmpty(cartResult.CartId))
                {
                    (Application.Current as App).CartId = cartResult.CartId;
                    await this.Navigation.PushAsync(new RegisterPage(deviceId.Value.ToString(), false));
                }
                else
                {
                    await this.DisplayAlert("SmartRetail", $"買い物を開始できません\n{cartResult.ErrorMessage}", "OK");

                    // 取引開始できない場合はログインへ戻る
                    await this.Navigation.PopAsync();
                }

                //インジケータを隠す
                SetIndicator(false);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}