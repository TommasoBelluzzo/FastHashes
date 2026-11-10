#!/usr/bin/env python3
"""Losslessly normalize FastHashes conformance fixtures.

Usage: python normalize_vectors.py PATH_TO/Solution/FastHashes.Tests/Data
       python normalize_vectors.py PATH_TO/Solution/FastHashes.Tests/Data --write

Default is a dry-run. With --write, originals are copied to a timestamped
backup ZIP before replacement. The audit is written to the parent directory.
"""
import argparse
import collections
import copy
import datetime
import json
import pathlib
import re
import shutil
import sys
import zipfile

NAME_RE = re.compile(r'^([a-z]+)-len-(\d+)-seed-([0-9A-Fa-f]+)$')
RENAME = {
    'FastHash32': {'sequential': 'increment'},
    'FastHash64': {'sequential': 'increment'},
    'XxHash32': {'sequential': 'increment'},
    'XxHash64': {'sequential': 'increment'},
    'Mx3Hash': {'ff': 'ones', 'random': 'mixed'},
    'SpookyHash32': {'randomlike': 'mixed'},
    'SpookyHash64': {'randomlike': 'mixed'},
    'SpookyHash128': {'randomlike': 'mixed'},
}


def load(path):
    with path.open('r', encoding='utf-8-sig') as stream:
        return json.load(stream)


def identity(case):
    match = NAME_RE.fullmatch(case['name'])
    if match is None:
        raise ValueError('Invalid case name: ' + repr(case['name']))
    family, written_length, written_seed = match.groups()
    length = len(case['input']) // 2
    if not re.fullmatch('[0-9A-Fa-f]*', case['input']) or len(case['input']) % 2:
        raise ValueError('Invalid hexadecimal input: ' + case['name'])
    if length != int(written_length) or int(written_seed, 16) != int(case['seed'], 16):
        raise ValueError('Incorrect length or seed: ' + case['name'])
    return family, length, int(case['seed'], 16)


def validate_family(case, old_family, new_family, mixed_inputs):
    data = bytes.fromhex(case['input'])
    if old_family == 'ff' and new_family == 'ones':
        valid = all(byte == 255 for byte in data)
    elif old_family == 'sequential' and new_family == 'increment':
        valid = all(byte == (i & 255) for i, byte in enumerate(data))
    elif old_family in ('random', 'randomlike') and new_family == 'mixed':
        # Verify every byte against the existing independently named Murmur corpus.
        valid = mixed_inputs.get(len(data)) == case['input']
    else:
        raise ValueError('Unsupported rename: ' + old_family + ' -> ' + new_family)
    if not valid:
        raise ValueError('Input family is not equivalent: ' + case['name'])


def semantic_multiset(cases):
    # Exclude names and order, but include every other case field, especially
    # input, seed, expected digest, and any algorithm-specific metadata.
    return collections.Counter(
        json.dumps({key: value for key, value in case.items() if key != 'name'},
                   sort_keys=True, separators=(',', ':'))
        for case in cases
    )


def normalize(path, mixed_inputs):
    source = load(path)
    if source.get('algorithm') != path.stem:
        raise ValueError(f'Algorithm/filename mismatch: {path.name}')
    original = copy.deepcopy(source)
    seen = set()
    renamed = 0
    for case in source['cases']:
        old_family, _, _ = identity(case)
        new_family = RENAME.get(path.stem, {}).get(old_family, old_family)
        if new_family != old_family:
            validate_family(case, old_family, new_family, mixed_inputs)
            case['name'] = new_family + case['name'][len(old_family):]
            renamed += 1
        if case['name'] in seen:
            raise ValueError(f'Duplicate case name: {path.name}: {case["name"]}')
        seen.add(case['name'])
    source['cases'].sort(key=lambda case: (identity(case)[1], identity(case)[0], identity(case)[2]))
    if semantic_multiset(original['cases']) != semantic_multiset(source['cases']):
        raise ValueError(f'Lossless semantic check failed: {path.name}')
    prior_metadata = {key: value for key, value in original.items() if key != 'cases'}
    current_metadata = {key: value for key, value in source.items() if key != 'cases'}
    if prior_metadata != current_metadata:
        raise ValueError(f'File metadata changed: {path.name}')
    if len(original['cases']) != len(source['cases']):
        raise ValueError(f'Case count changed: {path.name}')
    changed = source != original
    return source, dict(file=path.name, cases=len(source['cases']), renamed=renamed,
                        changed=changed, first=source['cases'][0]['name'],
                        last=source['cases'][-1]['name'])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=pathlib.Path)
    parser.add_argument('--write', action='store_true', help='Write validated files after making a backup ZIP')
    args = parser.parse_args()
    directory = args.directory.resolve()
    paths = sorted(directory.glob('*.json'))
    if not paths:
        parser.error('No JSON fixtures found at ' + str(directory))
    murmur_path = directory / 'MurmurHash32.json'
    if not murmur_path.is_file():
        parser.error('Missing MurmurHash32.json, required for mixed-corpus verification')
    murmur = load(murmur_path)
    mixed = {}
    for case in murmur['cases']:
        if identity(case)[0] == 'mixed':
            length = len(case['input']) // 2
            if length in mixed and mixed[length] != case['input']:
                raise ValueError('Conflicting Murmur mixed inputs at length ' + str(length))
            mixed[length] = case['input']
    results = []
    transformed = {}
    for path in paths:
        data, status = normalize(path, mixed)
        results.append(status)
        if status['changed']:
            transformed[path] = data
    report = ['FastHashes conformance fixture normalization',
              'Source: ' + str(directory),
              'Files checked: ' + str(len(results)),
              'Files changed: ' + str(len(transformed)),
              'Cases checked: ' + str(sum(item['cases'] for item in results)),
              'Cases renamed: ' + str(sum(item['renamed'] for item in results)),
              'All semantic mapping and metadata checks: PASS', '']
    for item in results:
        report.append(f'{item["file"]}: cases={item["cases"]}, renamed={item["renamed"]}, '
                      f'changed={item["changed"]}, first={item["first"]}, last={item["last"]}')
    if args.write and transformed:
        stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
        backup = directory.parent / ('vectors-backup-' + stamp + '.zip')
        with zipfile.ZipFile(backup, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
            for path in transformed:
                archive.write(path, arcname=path.name)
        report.append('\nBackup: ' + str(backup))
        for path, data in transformed.items():
            temp = path.with_suffix('.json.tmp')
            with temp.open('w', encoding='utf-8', newline='\n') as stream:
                json.dump(data, stream, ensure_ascii=False, indent=2)
                stream.write('\n')
            temp.replace(path)
        report.append('Write: completed')
    else:
        report.append('\nWrite: not requested (dry run)' if not args.write else '\nWrite: no changes necessary')
    audit = directory.parent / 'vectors-normalization-audit.txt'
    audit.write_text('\n'.join(report) + '\n', encoding='utf-8')
    print('\n'.join(report[:8]))
    print('Audit: ' + str(audit))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as exc:
        print('Normalization FAILED: ' + str(exc), file=sys.stderr)
        raise SystemExit(1)
