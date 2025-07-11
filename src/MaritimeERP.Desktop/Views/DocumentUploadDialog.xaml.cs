using System.Windows;
using System.Windows.Controls;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MaritimeERP.Core.Entities;
using System.Linq;
using System.IO;
using System;

namespace MaritimeERP.Desktop.Views
{
    public partial class DocumentUploadDialog : Window
    {
        public DocumentUploadDialog(string filePath, IDocumentService documentService, IShipService shipService)
        {
            InitializeDialog(filePath, documentService, shipService);
        }

        private async void InitializeDialog(string filePath, IDocumentService documentService, IShipService shipService)
        {
            // Basic window setup
            Title = "Upload Document";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            // Create UI
            var mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerText = new TextBlock
            {
                Text = "Upload Document",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(headerText, 0);
            mainGrid.Children.Add(headerText);

            // Content area
            var contentPanel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(contentPanel, 1);

            // File info
            contentPanel.Children.Add(new TextBlock
            {
                Text = $"File: {System.IO.Path.GetFileName(filePath)}",
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Document name
            contentPanel.Children.Add(new TextBlock { Text = "Document Name:", Margin = new Thickness(0, 10, 0, 5) });
            var nameTextBox = new TextBox
            {
                Height = 35,
                Padding = new Thickness(8),
                Text = System.IO.Path.GetFileNameWithoutExtension(filePath)
            };
            contentPanel.Children.Add(nameTextBox);

            // Category
            contentPanel.Children.Add(new TextBlock { Text = "Category:", Margin = new Thickness(0, 10, 0, 5) });
            var categoryComboBox = new ComboBox
            {
                Height = 35,
                Padding = new Thickness(8)
            };
            contentPanel.Children.Add(categoryComboBox);

            // Ship
            contentPanel.Children.Add(new TextBlock { Text = "Ship (Optional):", Margin = new Thickness(0, 10, 0, 5) });
            var shipComboBox = new ComboBox
            {
                Height = 35,
                Padding = new Thickness(8)
            };
            contentPanel.Children.Add(shipComboBox);

            // Load data
            try
            {
                var categories = await documentService.GetAllCategoriesAsync();
                categoryComboBox.ItemsSource = categories;
                categoryComboBox.DisplayMemberPath = "Name";
                categoryComboBox.SelectedValuePath = "Id";
                if (categories.Any()) categoryComboBox.SelectedIndex = 0;

                var ships = await shipService.GetAllShipsAsync();
                shipComboBox.ItemsSource = ships;
                shipComboBox.DisplayMemberPath = "ShipName";
                shipComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            mainGrid.Children.Add(contentPanel);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);

            var uploadButton = new Button
            {
                Content = "Upload",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White
            };

            uploadButton.Click += async (sender, e) =>
            {
                try
                {
                    uploadButton.IsEnabled = false;
                    uploadButton.Content = "Uploading...";

                    var selectedCategory = categoryComboBox.SelectedItem as DocumentCategory;
                    var selectedShip = shipComboBox.SelectedItem as Ship;

                    if (selectedCategory == null)
                    {
                        MessageBox.Show("Please select a document category.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                    {
                        MessageBox.Show("Please enter a document name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var document = new Document
                    {
                        Name = nameTextBox.Text.Trim(),
                        FileName = System.IO.Path.GetFileName(filePath),
                        CategoryId = selectedCategory.Id,
                        ShipId = selectedShip?.Id,
                        UploadedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsApproved = false
                    };

                    // Read the file and create the document
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        await documentService.CreateDocumentAsync(document, fileStream, System.IO.Path.GetFileName(filePath));
                    }

                    MessageBox.Show("Document uploaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error uploading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    uploadButton.IsEnabled = true;
                    uploadButton.Content = "Upload";
                }
            };

            cancelButton.Click += (sender, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(uploadButton);
            buttonPanel.Children.Add(cancelButton);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }
    }
} 