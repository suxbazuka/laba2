using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace EnemyEditor
{
    public partial class MainWindow : Window
    {
        private CEnemyTemplateList enemyList;
        private List<EnemyIcon> enemyIcons;

        // Переменные для отслеживания выбранного врага и предотвращения рекурсии
        private string _currentSelectedEnemyName = null;
        private bool _isUpdatingSelection = false;

        public MainWindow()
        {
            InitializeComponent();
            enemyList = new CEnemyTemplateList();
            enemyIcons = new List<EnemyIcon>();
        }

        // ================== Работа с иконками ==================

        private void ChooseIconsFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dlg = new OpenFolderDialog
            {
                Title = "Выберите папку с иконками противников"
            };

            if (dlg.ShowDialog() == true)
            {
                enemyIcons.Clear();
                IconsListBox.Items.Clear();

                LoadIconsFromFolder(dlg.FolderName);
                FillIconsListBox();
            }
        }

        public void LoadIconsFromFolder(string path)
        {
            string filter = "*.png";
            string[] files = Directory.GetFiles(path, filter);

            foreach (string file in files)
            {
                enemyIcons.Add(new EnemyIcon
                {
                    Name = Path.GetFileName(file),
                    ImagePath = file
                });
            }
        }

        private void FillIconsListBox()
        {
            IconsListBox.Items.Clear();

            foreach (EnemyIcon icon in enemyIcons)
            {
                var imageSource = LoadImageSafe(icon.ImagePath);
                if (imageSource == null) continue;

                Image image = new Image
                {
                    Source = imageSource,
                    Tag = icon.Name, // ИСПРАВЛЕНИЕ: Сохраняем имя файла в Tag
                    Height = 64,
                    Width = 64,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(4)
                };

                IconsListBox.Items.Add(image);
            }
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox iconHolder = sender as ListBox;

            // ИСПРАВЛЕНИЕ: Достаем имя из Tag, а не из ToString()
            if (iconHolder.SelectedItem is Image selectedImage && selectedImage.Tag is string iconName)
            {
                IconNameTextBox.Text = iconName;
                MainEnemyIcon.Source = selectedImage.Source;
            }
        }

        private BitmapImage FindIconSourceByName(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return null;

            EnemyIcon icon = enemyIcons.FirstOrDefault(i =>
                string.Equals(i.Name, iconName, StringComparison.OrdinalIgnoreCase));

            if (icon == null) return null;

            return LoadImageSafe(icon.ImagePath);
        }

        private BitmapImage LoadImageSafe(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        // ================== Работа со списком противников ==================

        private void RefreshEnemiesListBox()
        {
            EnemiesListBox.Items.Clear();
            foreach (string name in enemyList.GetListOfEnemyNames())
            {
                EnemiesListBox.Items.Add(name);
            }
        }

        // НОВЫЙ МЕТОД: Автосохранение изменений текущего врага
        private void TrySaveCurrentEnemyChanges()
        {
            if (string.IsNullOrEmpty(_currentSelectedEnemyName)) return;

            string newName = NameTextBox.Text.Trim();
            string iconName = IconNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(newName)) return;

            if (!int.TryParse(BaseLifeTextBox.Text, out int baseLife) ||
                !double.TryParse(LifeModifierTextBox.Text, out double lifeModifier) ||
                !int.TryParse(BaseGoldTextBox.Text, out int baseGold) ||
                !double.TryParse(GoldModifierTextBox.Text, out double goldModifier) ||
                !double.TryParse(SpawnChanceTextBox.Text, out double spawnChance))
            {
                return; // Игнорируем невалидные данные при автосохранении
            }

            if (newName != _currentSelectedEnemyName && enemyList.GetEnemyByName(newName) != null)
            {
                return; // Имя занято
            }

            bool updated = enemyList.UpdateEnemy(_currentSelectedEnemyName, newName, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance);

            if (updated && newName != _currentSelectedEnemyName)
            {
                _currentSelectedEnemyName = newName;
                _isUpdatingSelection = true;
                RefreshEnemiesListBox();
                EnemiesListBox.SelectedItem = newName;
                _isUpdatingSelection = false;
            }
        }

        private void EnemiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            // Сохраняем изменения предыдущего выбранного врага
            if (!string.IsNullOrEmpty(_currentSelectedEnemyName))
            {
                TrySaveCurrentEnemyChanges();
            }

            if (EnemiesListBox.SelectedItem == null)
            {
                _currentSelectedEnemyName = null;
                ClearFields();
                return;
            }

            string name = EnemiesListBox.SelectedItem.ToString();
            _currentSelectedEnemyName = name;

            CEnemyTemplate enemy = enemyList.GetEnemyByName(name);
            if (enemy == null) return;

            NameTextBox.Text = enemy.Name;
            IconNameTextBox.Text = enemy.IconName;
            BaseLifeTextBox.Text = enemy.BaseLife.ToString();
            LifeModifierTextBox.Text = enemy.LifeModifier.ToString();
            BaseGoldTextBox.Text = enemy.BaseGold.ToString();
            GoldModifierTextBox.Text = enemy.GoldModifier.ToString();
            SpawnChanceTextBox.Text = enemy.SpawnChance.ToString();

            MainEnemyIcon.Source = FindIconSourceByName(enemy.IconName);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            TrySaveCurrentEnemyChanges(); // Сохраняем изменения текущего, если есть

            string name = NameTextBox.Text.Trim();
            string iconName = IconNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя противника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(BaseLifeTextBox.Text, out int baseLife) ||
                !double.TryParse(LifeModifierTextBox.Text, out double lifeModifier) ||
                !int.TryParse(BaseGoldTextBox.Text, out int baseGold) ||
                !double.TryParse(GoldModifierTextBox.Text, out double goldModifier) ||
                !double.TryParse(SpawnChanceTextBox.Text, out double spawnChance))
            {
                MessageBox.Show("Проверьте правильность заполнения числовых полей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (enemyList.GetEnemyByName(name) != null)
            {
                MessageBox.Show("Противник с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            enemyList.AddEnemy(name, iconName, baseLife, lifeModifier, baseGold, goldModifier, spawnChance);

            _isUpdatingSelection = true;
            RefreshEnemiesListBox();
            EnemiesListBox.SelectedItem = name;
            _isUpdatingSelection = false;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem == null) return;

            string nameToRemove = EnemiesListBox.SelectedItem.ToString();
            _currentSelectedEnemyName = null;

            enemyList.DeleteEnemyByName(nameToRemove);

            _isUpdatingSelection = true;
            RefreshEnemiesListBox();
            _isUpdatingSelection = false;

            ClearFields();
        }

        private void ClearFields()
        {
            NameTextBox.Text = "";
            IconNameTextBox.Text = "";
            BaseLifeTextBox.Text = "";
            LifeModifierTextBox.Text = "";
            BaseGoldTextBox.Text = "";
            GoldModifierTextBox.Text = "";
            SpawnChanceTextBox.Text = "";
            MainEnemyIcon.Source = null;
        }

        // ================== Сохранение / Загрузка ==================

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            TrySaveCurrentEnemyChanges(); // Сохраняем перед записью в файл

            SaveFileDialog dlg = new SaveFileDialog
            {
                FileName = "enemies",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json"
            };

            if (dlg.ShowDialog() == true)
            {
                enemyList.SaveToJson(dlg.FileName);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                FileName = "enemies",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json"
            };

            if (dlg.ShowDialog() == true)
            {
                enemyList.LoadFromJson(dlg.FileName);

                _currentSelectedEnemyName = null;
                _isUpdatingSelection = true;
                RefreshEnemiesListBox();
                _isUpdatingSelection = false;

                ClearFields();
            }
        }
    }
}