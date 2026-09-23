#!/usr/bin/env python3

import re
import sys
from pathlib import Path
from dataclasses import dataclass, field


@dataclass
class CheckResult:
    errors: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    def add_error(self, message: str):
        self.errors.append(f"❌ {message}")

    def add_warning(self, message: str):
        self.warnings.append(f"⚠️  {message}")

    @property
    def is_success(self) -> bool:
        return len(self.errors) == 0


def read_file(path: Path) -> str:
    try:
        return path.read_text()
    except Exception:
        return ""


def iter_controllers(source_path: Path) -> list[Path]:
    """Все контроллеры сервиса, исключая сгенерированные папки obj/bin."""
    return [
        p for p in source_path.rglob("*Controller.cs")
        if not any(part in ("obj", "bin") for part in p.parts)
    ]


def check_thin_controllers(source_path: Path, result: CheckResult):
    """Конвенция 1: Контроллеры не обращаются к DbContext и репозиториям напрямую"""
    db_context_patterns = [
        r'DbContext',
        r'GetService<.*DbContext',
        r'AddScoped<.*DbContext',
        r'AddDbContext',
        r'\.Database\.',
        r'new \w+DbContext',
    ]

    for controller in iter_controllers(source_path):
        content = read_file(controller)

        if any(re.search(p, content) for p in db_context_patterns):
            result.add_error(
                f"[{controller.name}] Контроллер не должен обращаться к DbContext напрямую. Вынесите работу с данными в сервис/репозиторий."
            )

        if re.search(r'private\s+readonly\s+I\w+Repository', content):
            result.add_error(
                f"[{controller.name}] Контроллер не должен использовать репозитории напрямую. Используйте сервис."
            )


def check_dto_isolation(source_path: Path, result: CheckResult):
    """Конвенция 2: EF Core-сущности из Models/* не используются как контракты HTTP API"""
    models_dir = source_path / "Models"
    if not models_dir.exists():
        return

    # Только сущности (class/record), без enum/interface — они допустимы в запросах-фильтрах.
    models = set()
    for model_file in models_dir.rglob("*.cs"):
        models.update(
            re.findall(r'\b(?:class|record)\s+(\w+)', read_file(model_file))
        )

    if not models:
        return

    model_re = re.compile(r'\b(' + '|'.join(sorted(models, key=len, reverse=True)) + r')\b')
    action_re = re.compile(
        r'\[Http\w*(?:\([^\]\n]*\))?\][^\n{]*\n(?P<sig>[\s\S]*?\)\s*(?:=>|\{))'
    )

    for controller in iter_controllers(source_path):
        content = read_file(controller)
        for match in action_re.finditer(content):
            if model_re.search(match.group("sig")):
                result.add_error(
                    f"[{controller.name}] EF Core-сущность из Models/ используется как контракт API. Передавайте DTO (конвенция 2)."
                )
                break


def check_dtos_not_in_controllers(source_path: Path, result: CheckResult):
    """Конвенция 3: DTO не объявляются внутри контроллеров; они группируются в файлах {Entity}Dtos.cs"""
    dto_re = re.compile(
        r'^\s*(?:(?:public|internal|sealed|abstract|readonly|static)\s+)*'
        r'(?:class|record)\s+(\w+(?:Dto|Request|Response|ViewModel))\b',
        re.MULTILINE,
    )

    for controller in iter_controllers(source_path):
        content_clean = re.sub(r'//.*', '', read_file(controller))
        match = dto_re.search(content_clean)
        if match:
            result.add_error(
                f"[{controller.name}] DTO '{match.group(1)}' объявлен внутри контроллера. Вынесите его в файл группировки {{Entity}}Dtos.cs (конвенция 3)."
            )


def check_dependency_injection(source_path: Path, result: CheckResult):
    """Конвенция 4: Контроллеры используют интерфейсы сервисов через DI"""
    for controller in iter_controllers(source_path):
        content = read_file(controller)

        if controller.name.startswith("Internal"):
            continue

        if re.search(r'public\s+class\s+\w+Controller', content):
            if not re.search(r'private\s+readonly\s+I\w+Service', content):
                result.add_error(
                    f"[{controller.name}] Контроллер должен использовать интерфейсы сервисов через DI (конвенция 4)."
                )


def check_internal_controllers(source_path: Path, result: CheckResult):
    """Конвенция 5: Internal-контроллеры имеют префикс Internal и маршрут internal/..."""
    for controller in iter_controllers(source_path):
        content = read_file(controller)

        # Внутренний контроллер (класс с префиксом Internal) обязан иметь маршрут internal/
        if re.search(r'class\s+Internal\w+Controller', content):
            if not re.search(r'\[Route\("internal/', content):
                result.add_error(
                    f"[{controller.name}] Internal-контроллер должен использовать Route(\"internal/...\") (конвенция 5)."
                )

        # Пользовательский контроллер не должен использовать маршрут internal/
        if re.search(r'class\s+\w+Controller\s*:', content) and not controller.name.startswith("Internal"):
            if re.search(r'\[Route\("internal/', content):
                result.add_error(
                    f"[{controller.name}] Пользовательский контроллер не должен использовать маршрут internal/ (конвенция 5)."
                )


def check_user_id_from_jwt(source_path: Path, result: CheckResult):
    """Конвенция 6: Идентификатор пользователя извлекается через User.GetUserId(), а не парсингом claims в контроллере"""
    direct_claim_patterns = [
        r'ClaimTypes\.[A-Za-z]+',
        r'\.FindFirstValue\s*\(',
        r'\.FindFirst\s*\(',
        r'\.FindAll\s*\(',
    ]

    for controller in iter_controllers(source_path):
        content = read_file(controller)

        # Сам extension-класс, определяющий GetUserId(), — не контроллер и не проверяется.
        if any(re.search(p, content) for p in direct_claim_patterns):
            result.add_error(
                f"[{controller.name}] Не парсите claims (ClaimTypes/FindFirst) напрямую. Используйте User.GetUserId() (конвенция 6)."
            )


def check_test_db(source_path: Path, result: CheckResult):
    """Конвенция 7: Тесты не настраивают DbContextOptionsBuilder напрямую, а используют общий helper (TestDb/TestHelper)"""
    shared_helpers = {"testdb.cs", "testhelper.cs", "testhelperfactory.cs", "testdbfactory.cs"}

    test_dirs = [
        d for d in source_path.rglob("*")
        if d.is_dir() and re.search(r'test', d.name, re.IGNORECASE)
        and not any(part in ("obj", "bin") for part in d.parts)
    ]

    for test_dir in test_dirs:
        for test_file in test_dir.rglob("*.cs"):
            if test_file.name.lower() in shared_helpers:
                continue

            content = read_file(test_file)
            if re.search(r'DbContextOptionsBuilder\b', content) or re.search(
                r'UseInMemoryDatabase|UseSqlite|UseSqlServer', content
            ):
                result.add_error(
                    f"[{test_file.relative_to(source_path)}] Не настраивайте DbContext напрямую в тестах. Используйте общий helper (TestDb/TestHelper) (конвенция 7)."
                )


def main():
    services = ["auth-service", "gateway", "report-service", "transaction-service"]
    source_paths = [Path(s) for s in services if Path(s).exists()]

    if not source_paths:
        print("❌ Не найдены сервисы проекта")
        sys.exit(1)

    checks = [
        ("Контроллеры не обращаются к DbContext/репозиториям", check_thin_controllers),
        ("EF Core-модели не используются как контракты API (DTO)", check_dto_isolation),
        ("DTO не объявляются внутри контроллеров", check_dtos_not_in_controllers),
        ("DI: контроллеры используют интерфейсы сервисов", check_dependency_injection),
        ("Internal-контроллеры и маршрут internal/", check_internal_controllers),
        ("Идентификатор пользователя через User.GetUserId()", check_user_id_from_jwt),
        ("Тесты используют общий helper TestDb", check_test_db),
    ]

    result = CheckResult()

    for source_path in source_paths:
        print(f"\n📁 Проверка {source_path}...")
        for i, (title, check) in enumerate(checks, 1):
            print(f"[{i}/{len(checks)}] {title}...")
            check(source_path, result)

    print("\n" + "=" * 60)

    if result.warnings:
        print("⚠️  Предупреждения:")
        for warning in result.warnings:
            print(f"  {warning}")

    if result.errors:
        print("❌ Ошибки конвенций:")
        for error in result.errors:
            print(f"  {error}")
        print("\n💡 Проверьте AGENTS.md для получения подробной информации о правилах")
        sys.exit(1)

    if result.warnings:
        print("✅ Проверки пройдены (с предупреждениями)")
    else:
        print("✅ Все проверки пройдены успешно!")
    sys.exit(0)


if __name__ == "__main__":
    main()
