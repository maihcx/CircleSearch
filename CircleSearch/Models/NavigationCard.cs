// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace CircleSearch.Models;

public class NavigationCard : INotifyPropertyChanged
{
    private string _nameKey;
    private string _descriptionKey;

    public string NameKey
    {
        get => _nameKey;
        init
        {
            _nameKey = value;
        }
    }

    public string DescriptionKey
    {
        get => _descriptionKey;
        init
        {
            _descriptionKey = value;
        }
    }

    public string Name
    {
        get => string.IsNullOrEmpty(NameKey)
            ? string.Empty
            : TranslationSource.Instance[NameKey];
    }

    public string Description
    {
        get => string.IsNullOrEmpty(DescriptionKey) 
            ? string.Empty 
            : TranslationSource.Instance[DescriptionKey];
    }

    public SymbolRegular Icon { get; init; }

    public Type PageType { get; init; }

    public event PropertyChangedEventHandler PropertyChanged;

    public NavigationCard()
    {
        TranslationSource.Instance.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
        };
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}