# AutoSigner
Утилита для автоматического подписания файлов определёнными пользователями и дальнейшей отправкой по каналам ViPNet, используя автопроцессинг ViPNet "Деловая почта".

Проблемы, которые решает данный проект:
- отправка автопроцессингом нескольких файлов (автоархивация)
- подписание каждого файла цифровой подписью определённого человека (в зависимости от папки нахождения)

Основное использование - запуск по графику на определённой папке примерно такой структуры:
- users
	- Иванова А
	- Петров Б

После запуска утилита собирает все найдённые по маске файлы, подписывает, при необходимости архивирует и складывает в отдельную папку для автопроцессинга.

Выбор подписи зависит от папки, в которой находился файл.
Соответствие папок и ключей для поиска подписи настраивается.
Поиск файлов производится только по указанным в конфигурации соответствиям.

Выглядит процесс примерно так:

SourceDirectory `->` SearchPattern `->` Signer `->` Results `->` DestinationDirectory `->` PostProcessor

Последняя версия [здесь](https://github.com/rgsomskbr/AutoSigner/releases/latest)

<br/>

## Конфигурация
|||
| ------------ | ------------ |
| SourceDirectory | исходная папка для обработки |
| DestinationDirectory | папка назначения |
| SubfoldersMode | метод обработки подпапок: <ul><li>**parse** *(обрабатывать содержимое как обычно)* [по умолчанию]</li><li>**skip** *(не обрабатывать подпапки)*</li></ul> |
| SearchPattern | маска поиска |
| Signer | скрипт подписания |
| Results | метод обработки результатов подписания: <ul><li>**pack** *(заархивировать подписанный и исходный файлы)* [по умолчанию]</li><li>**move** *(перенести только подписанный файл)*</li><li>**move_pack** *(заархивировать только подписанный файл)*</li></ul>
| PostProcessor | скрипт, вызываемый **после** окончания |
| FolderKeyMap | таблица соответствий подпапок ключам поиска |
| ConsoleCodePage | кодовая страница для чтения результатов запуска скриптов *(по умолчанию "cp866")*|
| LogFile | путь к папке для хранения логов *(если не указан, лог не ведётся)* |

<br/>

#### SourceDirectory
Путь к папке, в которой хранятся подпапки пользователей.

#### DestinationDirectory
Путь к папке, в которую необходимо помещать результаты (например, папка автопроцессинга).

#### SubfoldersMode
Метод обработки подпапок. При использовании `parse` всё содержимое подпапки и всех вложенных подпапок обрабатывается как обычно. При указании `skip` подпапки **первого уровня** исключаются из обработки вместе с их содержимым.

#### SearchPattern
Регулярное выражение, используемое как маска для выбора файлов. Если не указано, то по умолчанию выбираются все файлы.

#### Signer
Путь к скрипту подписания. По завершению работы должен вывести в консоль последней строчкой путь к файлу с подписью.

#### Results
Метод обработки результатов. При подписании отсоединённой подписью на выходе создаётся дополнительный файл. Однако, автопроцессинг не обрабатывает несколько файлов сразу. По этой причине, при указании значения `pack` AutoSigner заархивирует исходный файл и файл с подписью в один архив и передаст далее для обработки. Исходный файл с подписью удаляется. Если же используется присоединённая подпись, то стоит указывать `move`, что бы AutoSigner передал дальше уже подписанный файл-контейнер без предварительной архивации вместе с исходным файлом. В случае если необходимо заархивировать только подписанный файл-контейнер без исходного файла, необходимо указать `move_pack`.

#### PostProcessor
Путь к скрипту, вызываемому **после** окончания обработки файлов. Вызывается после того, как итоговый файл будет скопирован в `DestinationDirectory`, до его удаления из исходной папки. Служит для создания цепочки обработки с другими утилитами.

#### FolderKeyMap
Таблица соответствий имён подпапок ключам для поиска электронной подписи, которые передаются в скрипт подписания.

#### ConsoleCodePage
Указывается стандартное имя кодовой страницы, которая используется для чтения результата запуска скриптов (последняя строчка в консоли при выходе). Если не указать, то по умолчанию используется страница `cp866`. В большинстве случаев этого достаточно, но если есть необходимость, можно изменить.

#### LogFile
Путь к папке, в которую помещаются логи действий. Каждый лог файл создаётся с меткой текущей даты. Если параметр не указан, то логи не фиксируются. Так же поддерживаются макросы переменных окружения (%TMP%, %APPDATA% и т.п.)

<br/>

Конфигурация хранится в файле config.json.
Обязательно использование экранированных символов (`"` превращется в `\"`, а `\` в `\\` и т.п.).

<br/>

## Командная строка
При указании путей для скриптов необходимо указывать набор параметров для передачи. Поддерживаются следующие макросы для подстановки:
|||
| ------------ | ------------ |
| %SRC% | путь до исходного файла |
| %DST% | путь до конечного файла *(доступен только после вызова скрипта подписания)* |
| %FLD% | путь подпапки из списка соответствий *(не реальной подпапки из файловой системы)* |
| %KEY% | ключ для поиска |


Если в параметрах встречается любой из указанных макросов, он заменяется в соответствии со своим значением. При необходимости каждый параметр надо обрамлять двойными кавычками самостоятельно.

<br/>

Например:

`"Signer": "super_signer.exe -param1 -param2 -name %SRC% -key %KEY%"`

При использовании скрипта будет запущена команда:

`super_signer.exe -param1 -param2 -name "c:\users\Иванова А\документ.docx" -key "ivanova_a@mail.ru"`

<br/>

## Результаты обработки
В случае успешной обработки всей цепочки, AutoSigner возвращает `0`. Если же в процессе возникает какая-либо ошибка, то возвращается код, отличный от `0` и пояснения в канал `error`.
Ошибкой является как внутренний сбой при работе утилиты, так и сбой при вызове внешних скриптов.
