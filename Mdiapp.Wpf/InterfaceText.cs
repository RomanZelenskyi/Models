using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mdiapp.Core;

namespace Mdiapp.Wpf
{
    public static class InterfaceText
    {
        private static readonly Dictionary<string,string> translations=new Dictionary<string,string>(StringComparer.Ordinal);
        public static void Configure(ApplicationSettings settings)
        {
            translations.Clear();if(settings==null)return;if(settings.Language.Equals("Ru",StringComparison.OrdinalIgnoreCase))return;
            var source=IniFile.Load(Path.Combine(settings.DataDirectory,"Ru","menu.ini"));var target=settings.Menu;foreach(string section in source.SectionNames)foreach(var entry in source.Entries(section)){string translated=target.Get(section,entry.Key);if(!string.IsNullOrWhiteSpace(entry.Value)&&!string.IsNullOrWhiteSpace(translated))translations[entry.Value]=translated;}
            if(settings.Language.Equals("En",StringComparison.OrdinalIgnoreCase))
            {
                string[] russian={"Файл","Модель","Редакторы","Результаты","Расчёт","Справка","Новый проект","Сохранить проект как…","Сохранить проект","Открыть проект INM…","Закрыть вкладку","Выход","Рассчитать","Остановить","Прочитать результаты…","Экспорт результатов…","Печать…","Руководство пользователя","О программе","Что нового","Начало","Комментарий","Сохранить как…","Сохранить","Открыть документ","Показывать при запуске","Копировать","Вставить из Excel","Очистить","Применить число точек","Применить размеры","Число точек","Характеристика","Параметр","Исходное значение","Дополнительные параметры","Режим","Масштаб","Коррекция","Основная характеристика","Переходные коэффициенты","Проект и расчёт","Режимы и параметры","Подключённые файлы","Выходные параметры и поправки","Редакторы файлов","Графики","Открыть редактор","Добавить DAT…","Удалить запись","Ось X","Ось Y","Показывать точки","Выбрать файл…","Выбрать папку двигателя…","Выбрать EXE…","Код файла","Ключ двигателя","Папка двигателя (bin / data / datar)","Расчётный EXE","Задача","Конфигурация","Условия","Флаг расчёта","Точности (через пробел)","Число режимов (1–10000)","Число аргументов (1–30)","Выходные параметры (номера через пробел)","Динамические факторы (0/1 через пробел)","Шаги идентификации","Управляющие параметры","Значения управляющих параметров","Номера поправок узлов","Значения поправок узлов","Номера многопараметрических поправок","Ряд поправок","Экспорт результатов","Формат","Имя файла","Текст TXT","HTML с графиками","Числовой RES","Проект открыт"};
                string[] english={"File","Model","Editors","Results","Calculation","Help","New project","Save project as…","Save project","Open INM project…","Close tab","Exit","Calculate","Stop","Read results…","Export results…","Print…","User guide","About","What's new","Start","Comment","Save as…","Save","Open document","Show at startup","Copy","Paste from Excel","Clear","Apply point count","Apply dimensions","Point count","Curve","Parameter","Initial value","Additional parameters","Mode","Scale","Correction","Primary curve","Transition coefficients","Project and calculation","Operating points and parameters","Included files","Output parameters and corrections","File editors","Graphs","Open editor","Add DAT…","Remove entry","X axis","Y axis","Show points","Choose file…","Choose engine folder…","Choose EXE…","File code","Engine key","Engine folder (bin / data / datar)","Solver EXE","Task","Configuration","Conditions","Calculation flag","Tolerances (space separated)","Operating points (1–10000)","Arguments (1–30)","Output parameter IDs (space separated)","Dynamic factors (0/1, space separated)","Identification steps","Control parameter IDs","Control parameter values","Node correction IDs","Node correction values","Multivariable correction IDs","Correction row","Export results","Format","File name","Plain text TXT","HTML with graphs","Numeric RES","Project opened"};
                for(int i=0;i<russian.Length;i++)translations[russian[i]]=english[i];
                translations["Выводимые параметры (номера через пробел)"]="Visible parameter IDs (space separated)";translations["Режимы результатов (номера через пробел; пусто — все)"]="Result row numbers (space separated; blank means all)";translations["Применить отбор"]="Apply selection";translations["Экспорт…"]="Export…";translations["Номер параметра"]="Parameter ID";translations["Предварительный просмотр печати"]="Print preview";translations["Сбросить масштаб"]="Reset zoom";translations["Второй параметр (красный)"]="Secondary parameter (red)";translations["Комментарий файла"]="File comment";translations["Нет"]="None";
                translations["Правка"]="Edit";translations["Вид"]="View";translations["Операции"]="Operations";translations["Настройки"]="Settings";translations["Окна"]="Windows";translations["Помощь"]="Help";translations["Панель инструментов"]="Toolbar";translations["Строка состояния"]="Status bar";translations["Нет открытых окон"]="No open windows";translations["Вырезать"]="Cut";translations["Вставить"]="Paste";translations["Настройки сохранения результатов"]="Result export settings";
                translations["Откройте проект INM или выберите редактор в меню. Окна модели представлены вкладками."]="Open an INM project or choose an editor from the menu. Projects use tabs.";
                translations["Создайте проект или откройте INM для подготовки данных, расчёта и просмотра результатов."]="Create a project or open INM to prepare input, run the solver and inspect results.";
            }
        }
        public static string Translate(string text)
        {
            if(text==null)return null;if(translations.TryGetValue(text,out string exact))return exact;foreach(var pair in translations.OrderByDescending(p=>p.Key.Length))if(text.StartsWith(pair.Key+" ",StringComparison.Ordinal)||text.StartsWith(pair.Key+":",StringComparison.Ordinal))return pair.Value+text.Substring(pair.Key.Length);return text;
        }
        public static void Apply(DependencyObject root)
        {
            if(root is TextBox||root is RichTextBox)return;
            if(root is Window window)window.Title=Translate(window.Title);
            if(root is TextBlock block)block.Text=Translate(block.Text);
            if(root is HeaderedContentControl header&&header.Header is string title)header.Header=Translate(title);
            if(root is HeaderedItemsControl items&&items.Header is string itemTitle)items.Header=Translate(itemTitle);
            if(root is ContentControl control&&control.Content is string content)control.Content=Translate(content);
            if(root is DataGrid grid){foreach(var column in grid.Columns)if(column.Header is string value)column.Header=Translate(value);return;}
            foreach(object child in LogicalTreeHelper.GetChildren(root))if(child is DependencyObject dependency)Apply(dependency);
        }
    }
}

